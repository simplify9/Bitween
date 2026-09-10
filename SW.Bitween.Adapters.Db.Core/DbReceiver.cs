using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SW.PrimitiveTypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SW.Bitween.Adapters.Db;

/// <summary>
/// The receiver half: rows out of the database and into Bitween as Xchanges.
///
/// Modes are Kafka Connect's vocabulary, because it is the one operators already have —
/// <c>bulk</c>, <c>incrementing</c>, <c>timestamp</c>, <c>timestamp+incrementing</c> — plus
/// <c>marker</c>, a processed-flag column, which is what enterprise integration tables actually
/// tend to use. None of them can see a DELETE. That is a property of polling, not of this adapter,
/// and the UI says so rather than letting someone discover it.
///
/// Two things make this safe to run at all:
///
/// * The cursor is HOST-HELD, through <c>IAdapterContext.SetStateAsync</c>. An adapter cannot keep
///   its own progress — the supervisor restarts it and the next instance may be elsewhere — and a
///   cursor that resets to the beginning replays every row already processed.
/// * The cursor advances per row, in DeleteFile, which the pipeline calls only after Bitween has
///   durably accepted that row. Advancing on read instead would lose rows on a crash; advancing
///   only at the end of a batch would replay the batch. Per row is the one that is merely
///   at-least-once rather than either-way-wrong.
/// </summary>
public abstract partial class DbResidentAdapterBase
{
    /// <summary>
    /// One poll's rows, held between ListFiles and the GetFile / DeleteFile calls that follow.
    ///
    /// Keyed rather than kept in a field because this instance is SHARED: two subscriptions bound
    /// to the same data source can poll concurrently, and a field would have one overwrite the
    /// other's batch halfway through. The key travels inside each row id.
    /// </summary>
    readonly ConcurrentDictionary<string, ReceiveBatch> batches = new();

    sealed class ReceiveBatch
    {
        public Dictionary<string, Dictionary<string, object>> Rows { get; } = new();
        public Dictionary<string, object> Cursors { get; } = new();
        public DateTimeOffset StartedOn { get; } = DateTimeOffset.UtcNow;
        public int Outstanding { get; set; }
    }

    /// <summary>
    /// Where the cursor was kept before it was scoped per subscription. Read as a fallback so an
    /// upgrade does not reset progress to the beginning and replay every row already processed;
    /// never written, so the first advance after an upgrade moves to the scoped name and the old
    /// one is simply left behind.
    /// </summary>
    const string LegacyCursorStateName = "receive.cursor";

    /// <summary>
    /// The host holds state per (adapter, instance, name), and the instance here is the DATA
    /// SOURCE — one connection shared by every subscription pointed at it. So the name has to
    /// carry the reader, or two subscriptions polling one connection share a cursor: whichever
    /// polls first advances it, and the rows it took are invisible to the other. No error, no
    /// warning, half the rows each.
    ///
    /// The subscription id arrives as a per-invocation value, which is why this is computed per
    /// call rather than held in a field — the field would belong to whichever subscription
    /// happened to call first.
    /// </summary>
    string CursorStateName()
    {
        var subscriptionId = Context.ValueOf(SubscriptionIdKey);
        return string.IsNullOrWhiteSpace(subscriptionId)
            ? LegacyCursorStateName
            : $"{LegacyCursorStateName}.{subscriptionId}";
    }

    /// <summary>
    /// Set by the host on every invocation. Its Bitween-side name is
    /// <c>StartupValuesFiller.SubscriptionIdKey</c>; the two are literals on either side of a
    /// process boundary rather than a shared constant, because the adapter is published on its
    /// own and shares no assembly with the host.
    /// </summary>
    const string SubscriptionIdKey = "__subscriptionId__";

    /// <summary>
    /// Which subscription inherited the unscoped cursor. Inheriting it is a one-time migration for
    /// the receiver that was already running, not a starting point for every reader added later:
    /// without this, a second subscription pointed at the same connection would begin life at
    /// wherever the first one had got to, silently skipping every row before that.
    ///
    /// A marker rather than a delete because the SDK's state API has no delete.
    /// </summary>
    const string CursorClaimStateName = "receive.cursor.inheritedBy";

    /// <summary>
    /// The saved cursor: this subscription's own, or — once, for whichever subscription asks
    /// first — the unscoped one left behind by a version that did not scope them.
    /// </summary>
    async Task<string> ReadCursorAsync()
    {
        var name = CursorStateName();
        var saved = await Context.GetStateAsync(name, Stopping);

        // Already has its own progress, or there is no subscription id to scope by — either way
        // there is nothing to inherit.
        if (!string.IsNullOrEmpty(saved) || name == LegacyCursorStateName) return saved;

        var legacy = await Context.GetStateAsync(LegacyCursorStateName, Stopping);
        if (string.IsNullOrEmpty(legacy)) return null;

        var subscriptionId = Context.ValueOf(SubscriptionIdKey);
        var claimedBy = await Context.GetStateAsync(CursorClaimStateName, Stopping);

        if (string.IsNullOrEmpty(claimedBy))
        {
            await Context.SetStateAsync(CursorClaimStateName, subscriptionId, Stopping);
            Logger.LogInformation(
                "Subscription {Subscription} has no cursor of its own, so it continues from the "
                + "unscoped {Legacy} this connection used before cursors were scoped. No later "
                + "subscription will inherit it.", subscriptionId, LegacyCursorStateName);
            return legacy;
        }

        // Claimed by this one already, and it has not accepted a row yet — so the inherited value
        // is still where it is up to.
        if (claimedBy == subscriptionId) return legacy;

        // Claimed by someone else: this is a new reader, and a new reader starts at the beginning.
        // Anything else would hand it another subscription's progress as its own.
        return null;
    }

    /// <summary>
    /// Nothing to do. The transaction a mark-processed statement might want cannot live here: the
    /// pipeline calls Initialize, then ListFiles, then a GetFile and DeleteFile per row, then
    /// Finalize — and holding one transaction open across all of that would pin a pooled connection
    /// for as long as Bitween takes to persist every row, on a shared instance serving other
    /// subscriptions at the same time. Each mark-processed runs in its own transaction instead,
    /// after Bitween has accepted the row, which is the at-least-once boundary anyway.
    /// </summary>
    public virtual Task Initialize()
    {
        PruneStaleBatches();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Polls for rows and returns one opaque id per row. The payload is read here, in one query,
    /// rather than re-selected per row: a round trip per row against a remote database turns a
    /// 500-row poll into 500 round trips.
    /// </summary>
    public virtual async Task<IEnumerable<string>> ListFiles()
    {
        var mode = (Options.ReceiveMode ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(mode))
            throw new InvalidOperationException(
                "This data source has no ReceiveMode, so it cannot be used as a receiver. Set one of "
                + "bulk, incrementing, timestamp, timestamp+incrementing or marker.");

        if (string.IsNullOrWhiteSpace(Options.ReceiveStatement))
            throw new InvalidOperationException(
                "ReceiveMode is set but ReceiveStatement is not, so there is nothing to poll with.");

        if (string.IsNullOrWhiteSpace(Options.KeyColumn))
            throw new InvalidOperationException(
                "KeyColumn is required for receiving: without it a row cannot be identified, so it "
                + "cannot be marked processed and it cannot be deduplicated.");

        var needsCursor = mode is "incrementing" or "timestamp" or "timestamp+incrementing";
        if (needsCursor && string.IsNullOrWhiteSpace(Options.CursorColumn))
            throw new InvalidOperationException(
                $"ReceiveMode '{mode}' follows a column, so CursorColumn is required.");

        if (mode is "bulk" or "marker" && string.IsNullOrWhiteSpace(Options.MarkProcessedStatement))
            throw new InvalidOperationException(
                $"ReceiveMode '{mode}' has no cursor, so MarkProcessedStatement is what stops the "
                + "same rows being read again on every poll. Set it, or use a cursor mode.");

        var parameters = new Dictionary<string, object>();
        if (needsCursor)
        {
            var saved = await ReadCursorAsync();
            parameters["cursor"] = ParseCursor(saved, mode);

            Logger.LogDebug("Polling from cursor {Cursor} ({Mode}).", saved ?? "(none)", mode);
        }

        var page = await QueryCore(new StatementRequest
        {
            Sql = Options.ReceiveStatement,
            Parameters = parameters,
            MaxRows = Options.ReceiveBatchSize,

            // The receive statement is configuration on the data source, not something a message
            // supplied, so it does not go through the ad-hoc gate — it IS the allow-list entry.
            Name = null
        }, adHocAllowed: true);

        // A page that filled exactly to the batch size leaves a cursor open behind it; the next
        // poll picks up from the saved cursor, so it is closed rather than carried.
        if (page.CursorId != null) await CloseCursor(page.CursorId);

        if (page.Rows.Count == 0) return Array.Empty<string>();

        var batchId = Guid.NewGuid().ToString("N");
        var batch = new ReceiveBatch { Outstanding = page.Rows.Count };
        var ids = new List<string>(page.Rows.Count);

        foreach (var row in page.Rows)
        {
            if (!row.TryGetValue(Options.KeyColumn, out var key) || key == null)
                throw new InvalidOperationException(
                    $"A row came back with no value in the key column '{Options.KeyColumn}'. The "
                    + "receive statement has to select it, spelled as the database returns it.");

            var id = $"{batchId}:{key}";
            batch.Rows[id] = row;

            if (needsCursor && row.TryGetValue(Options.CursorColumn, out var cursorValue))
                batch.Cursors[id] = cursorValue;

            ids.Add(id);
        }

        batches[batchId] = batch;
        Logger.LogInformation("Polled {Count} row(s) in batch {Batch}.", ids.Count, batchId);
        return ids;
    }

    /// <summary>The row, as JSON, exactly as the query returned it.</summary>
    public virtual Task<XchangeFile> GetFile(string fileId)
    {
        var row = Locate(fileId, out _);
        var json = JsonConvert.SerializeObject(row);

        // Named after the row, so an operator looking at an Xchange can see which one it was.
        return Task.FromResult(new XchangeFile(json, $"{KeyOf(fileId)}.json"));
    }

    /// <summary>
    /// Called once Bitween has durably accepted the row, and therefore the point at which progress
    /// becomes real: the mark-processed statement runs, and the cursor advances past this row.
    ///
    /// Both, in that order. Advancing the cursor first would skip a row whose marking failed.
    /// </summary>
    public virtual async Task DeleteFile(string fileId)
    {
        var row = Locate(fileId, out var batch);
        var key = KeyOf(fileId);

        if (!string.IsNullOrWhiteSpace(Options.MarkProcessedStatement))
        {
            var parameters = new Dictionary<string, object> { ["key"] = RawKey(row) };

            // Every column is offered as a parameter too, so a mark statement can use more than the
            // key — a status column, a batch id, the row's own timestamp.
            foreach (var kv in row) parameters[kv.Key] = kv.Value;

            await Execute(new StatementRequest
            {
                Sql = Options.MarkProcessedStatement,
                Parameters = parameters
            }, adHocAllowed: true);
        }

        if (batch.Cursors.TryGetValue(fileId, out var cursorValue) && cursorValue != null)
            await Context.SetStateAsync(CursorStateName(), FormatCursor(cursorValue), Stopping);

        batch.Rows.Remove(fileId);
        batch.Outstanding--;

        Logger.LogDebug("Row {Key} processed; {Outstanding} left in its batch.", key, batch.Outstanding);
    }

    /// <summary>
    /// Drops whatever the run did not get through. Rows left here were never accepted by Bitween
    /// and the cursor never moved past them, so the next poll reads them again — which is the
    /// correct at-least-once behaviour, not a leak.
    /// </summary>
    public virtual Task Finalize()
    {
        foreach (var kv in batches.ToArray())
            if (kv.Value.Outstanding <= 0 || kv.Value.Rows.Count == 0)
                batches.TryRemove(kv.Key, out _);

        PruneStaleBatches();
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------ helpers

    Dictionary<string, object> Locate(string fileId, out ReceiveBatch batch)
    {
        var separator = fileId?.IndexOf(':') ?? -1;
        if (separator <= 0)
            throw new ArgumentException($"'{fileId}' is not a row id this receiver handed out.", nameof(fileId));

        var batchId = fileId.Substring(0, separator);
        if (!batches.TryGetValue(batchId, out batch))
            throw new InvalidOperationException(
                $"Batch {batchId} is no longer held. Either the run already finished, or the adapter "
                + "restarted between the listing and this call — in which case the rows were never "
                + "acknowledged and the next poll will read them again.");

        if (!batch.Rows.TryGetValue(fileId, out var row))
            throw new InvalidOperationException($"Row '{fileId}' has already been processed in this run.");

        return row;
    }

    static string KeyOf(string fileId) => fileId.Substring(fileId.IndexOf(':') + 1);

    object RawKey(Dictionary<string, object> row) =>
        row.TryGetValue(Options.KeyColumn, out var value) ? value : null;

    /// <summary>
    /// A cursor comes back from the host as text, and has to go into the query as the type the
    /// column compares against — a string on one side of a timestamp comparison silently matches
    /// nothing on some engines rather than failing.
    /// </summary>
    object ParseCursor(string saved, string mode)
    {
        if (string.IsNullOrEmpty(saved))
            // The floor for a first run. Not null: null in a WHERE comparison excludes every row,
            // so a first poll would find nothing and never start.
            return mode.StartsWith("timestamp", StringComparison.Ordinal)
                ? (object)new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                : 0L;

        if (mode.StartsWith("timestamp", StringComparison.Ordinal))
            return DateTime.TryParse(saved, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : throw new InvalidOperationException(
                    $"The saved cursor '{saved}' is not a timestamp. Clear the adapter state for "
                    + "this data source, or switch the mode to match the column.");

        return long.TryParse(saved, out var number) ? number : (object)saved;
    }

    static string FormatCursor(object value) => value switch
    {
        DateTime dt => dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value?.ToString()
    };

    /// <summary>
    /// A batch whose run died mid-way — the job was cancelled, the node went down — is otherwise
    /// held for the life of the process, and it holds every row of that poll in memory.
    /// </summary>
    void PruneStaleBatches()
    {
        var deadline = DateTimeOffset.UtcNow.AddHours(-1);
        foreach (var kv in batches.ToArray())
            if (kv.Value.StartedOn < deadline && batches.TryRemove(kv.Key, out _))
                Logger.LogWarning(
                    "Dropped receive batch {Batch}: it was started over an hour ago and never "
                    + "finished. Its {Count} unacknowledged row(s) will be read again.",
                    kv.Key, kv.Value.Rows.Count);
    }
}
