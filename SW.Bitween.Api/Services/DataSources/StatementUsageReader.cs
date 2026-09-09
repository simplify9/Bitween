using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SW.Bitween.Services.DataSources;

/// <summary>
/// Which subscriptions name which statements.
///
/// Answering it needs a scan rather than a join, because a subscription names a statement inside
/// its adapter properties — a JSON column — and those are not queryable in a way that is the same
/// on PostgreSQL, MySQL and SQL Server. The scan is bounded by the subscriptions bound to ONE data
/// source, which is a small number by construction, and it happens on a screen rather than in the
/// message path.
///
/// It exists because "is anything still using this?" is the question that decides whether a
/// statement can be deleted, and without an answer nobody ever deletes anything.
/// </summary>
public class StatementUsageReader(BitweenDbContext dbContext)
{
    /// <summary>The adapter property that names a statement. Must match what the adapter reads.</summary>
    public const string StatementKey = "Statement";
    public const string OperationKey = "Operation";

    /// <summary>
    /// Usage for every statement of one data source, keyed by statement name, case-insensitively —
    /// the adapter resolves names that way, so counting them any other way would report a statement
    /// as unused while a subscription happily runs it.
    /// </summary>
    public async Task<Dictionary<string, List<DataSourceStatementUsageEntry>>> ForDataSourceAsync(
        int dataSourceId)
    {
        var subscriptions = await dbContext.Set<Subscription>()
            .Where(s => s.DataSourceId == dataSourceId)
            .AsNoTracking()
            .ToListAsync();

        var usage = new Dictionary<string, List<DataSourceStatementUsageEntry>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var subscription in subscriptions)
        {
            Record(usage, subscription, "Handler", subscription.HandlerProperties);
            Record(usage, subscription, "Mapper", subscription.MapperProperties);
            Record(usage, subscription, "Receiver", subscription.ReceiverProperties);
        }

        return usage;
    }

    static void Record(Dictionary<string, List<DataSourceStatementUsageEntry>> usage,
        Subscription subscription, string role, IReadOnlyDictionary<string, string> properties)
    {
        if (properties == null) return;

        var name = Value(properties, StatementKey);
        if (string.IsNullOrWhiteSpace(name)) return;

        if (!usage.TryGetValue(name, out var entries))
            usage[name] = entries = new List<DataSourceStatementUsageEntry>();

        entries.Add(new DataSourceStatementUsageEntry
        {
            SubscriptionId = subscription.Id,
            SubscriptionName = subscription.Name,
            Role = role,
            Operation = Value(properties, OperationKey) ?? "query",
            Inactive = subscription.Inactive
        });
    }

    /// <summary>
    /// Case-insensitive, because adapter properties are matched that way when they are bound to an
    /// adapter's settings class, and an operator who typed "statement" should not silently get a
    /// usage count of zero on a statement that is in fact used.
    /// </summary>
    static string Value(IReadOnlyDictionary<string, string> properties, string key) =>
        properties.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
            .Value;
}
