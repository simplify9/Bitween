using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Domain.Gateway;
using SW.Bitween.Services.Cluster;
using SW.Serverless.Resident;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SW.Bitween.Services.DataSources;

/// <summary>
/// Reconciles running adapters against the data sources this node should be serving.
///
/// Desired state is the set of active <see cref="DataSource"/> rows; actual state is what the
/// resident host is running. The loop starts what is missing, stops what is no longer wanted, and
/// restarts what has changed — the same shape as the Quartz schedule reconciliation that already
/// exists for subscriptions.
///
/// PLACEMENT. A broker connection is exclusive — two nodes consuming the same queue is duplicate
/// processing, which is the failure this whole design exists to prevent. So every data source is
/// owned through a lease, granted per data source rather than globally: whichever node wins each
/// race owns that source, so load spreads without anyone scheduling it.
///
/// A lease is checked, not assumed. Before every reconcile the supervisor revalidates what it
/// believes it owns, because holding the lock is not the same as still being the current owner —
/// a node paused long enough for its queue to be released and reclaimed would otherwise carry on
/// consuming. Losing a lease stops its adapter immediately.
/// </summary>
public class BusProviderSupervisor : BackgroundService
{
    private static readonly TimeSpan ReconcileInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly IResidentAdapterHost _adapters;
    private readonly ILeaderElection _election;
    private readonly ILogger<BusProviderSupervisor> _logger;

    // What we last started, and the configuration fingerprint it was started with.
    private readonly Dictionary<int, string> _running = new();

    // What this node currently owns. Nothing runs without an entry here.
    private readonly Dictionary<int, IResourceLease> _leases = new();

    public BusProviderSupervisor(IServiceProvider serviceProvider, IResidentAdapterHost adapters,
        ILeaderElection election, ILogger<BusProviderSupervisor> logger)
    {
        _serviceProvider = serviceProvider;
        _adapters = adapters;
        _election = election;
        _logger = logger;
    }

    private static string ResourceOf(DataSource dataSource) => $"datasource.{dataSource.Id}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Never let one bad reconcile end the loop; the next pass retries everything.
                _logger.LogError(ex, "Bus provider reconciliation failed.");
            }

            try { await Task.Delay(ReconcileInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        // Release on the way out so a rolling restart hands ownership over in seconds rather than
        // leaving the next node to wait for the broker to time the connection out.
        foreach (var dataSourceId in _leases.Keys.ToList())
            await ReleaseAsync(dataSourceId);
    }

    /// <summary>
    /// One reconciliation pass. Public because "reconcile now" is a real operation — the same
    /// shape as the receivenow endpoint for subscriptions — and because a supervisor whose only
    /// entry point is a thirty-second timer cannot be tested at all.
    /// </summary>
    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BitweenDbContext>();

        var desired = await dbContext.Set<DataSource>()
            .Where(d => !d.Inactive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Endpoint configuration lives on the gateways, so an adapter is told what to consume by
        // the gateways pointing at it rather than by the data source alone.
        var endpoints = await dbContext.Set<BusGateway>()
            .Where(g => g.DataSourceId != null && !g.Inactive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Revalidate first. A lease that is no longer current must stop its adapter before
        // anything else happens this pass, not after.
        await ReleaseLostLeasesAsync(cancellationToken);

        foreach (var dataSource in desired)
        {
            if (!await EnsureOwnedAsync(dataSource, cancellationToken))
            {
                // Owned by another node. If we were running it, we are not any more.
                await StopIfRunningAsync(dataSource.Id, dataSource.AdapterId, cancellationToken);
                continue;
            }

            var startupValues = BuildStartupValues(dataSource, endpoints);
            var fingerprint = Fingerprint(dataSource, startupValues);

            if (_running.TryGetValue(dataSource.Id, out var current))
            {
                if (current == fingerprint) continue;

                _logger.LogInformation("Data source {Name} changed; restarting its adapter.", dataSource.Name);
                await _adapters.StopAsync(dataSource.AdapterId, dataSource.Id.ToString(),
                    drain: true, cancellationToken);
                _running.Remove(dataSource.Id);
            }

            try
            {
                await _adapters.StartExclusiveAsync(new AdapterSpec
                {
                    AdapterId = dataSource.AdapterId,
                    // The instance key IS the data source id, which is how the sink knows which
                    // gateway an inbound message belongs to.
                    InstanceKey = dataSource.Id.ToString(),
                    StartupValues = startupValues
                }, cancellationToken);

                _running[dataSource.Id] = fingerprint;
                _logger.LogInformation("Data source {Name} running on adapter {AdapterId}.",
                    dataSource.Name, dataSource.AdapterId);
            }
            catch (Exception ex)
            {
                // One unreachable broker must not stop the others from starting.
                _logger.LogError(ex, "Could not start adapter {AdapterId} for data source {Name}.",
                    dataSource.AdapterId, dataSource.Name);
            }
        }

        // Anything running that is no longer desired. Its lease goes too — holding a lock on a
        // data source nobody wants would block a node that later does.
        var wanted = desired.Select(d => d.Id).ToHashSet();
        foreach (var id in _running.Keys.Where(k => !wanted.Contains(k)).ToList())
        {
            await StopIfRunningAsync(id, adapterId: null, cancellationToken);
            await ReleaseAsync(id);
        }

        await WriteBackHealthAsync(dbContext, cancellationToken);
    }

    /// <summary>
    /// True when this node owns the data source. Acquires the lease if it does not hold one, and
    /// revalidates the term if it does.
    /// </summary>
    private async Task<bool> EnsureOwnedAsync(DataSource dataSource, CancellationToken cancellationToken)
    {
        if (_leases.TryGetValue(dataSource.Id, out var held))
        {
            if (await held.ValidateAsync(cancellationToken)) return true;

            _logger.LogWarning(
                "Lease on data source {Name} is no longer current; another node has taken it.",
                dataSource.Name);

            await ReleaseAsync(dataSource.Id);
            return false;
        }

        var lease = await _election.TryAcquireAsync(ResourceOf(dataSource), cancellationToken);
        if (lease == null) return false;

        _leases[dataSource.Id] = lease;
        return true;
    }

    private async Task ReleaseLostLeasesAsync(CancellationToken cancellationToken)
    {
        foreach (var (dataSourceId, lease) in _leases.ToList())
        {
            if (await lease.ValidateAsync(cancellationToken)) continue;

            _logger.LogWarning("Lost the lease on data source {DataSourceId} at term {Term}; stopping it.",
                dataSourceId, lease.Term);

            // Stopped WITHOUT draining: another node may already be consuming, so finishing
            // in-flight work here risks processing the same messages twice.
            await StopIfRunningAsync(dataSourceId, adapterId: null, cancellationToken, drain: false);
            await ReleaseAsync(dataSourceId);
        }
    }

    private async Task StopIfRunningAsync(int dataSourceId, string adapterId,
        CancellationToken cancellationToken, bool drain = true)
    {
        if (!_running.ContainsKey(dataSourceId)) return;

        adapterId ??= _adapters.Describe()
            .FirstOrDefault(h => h.InstanceKey == dataSourceId.ToString())?.AdapterId;

        if (adapterId != null)
            await _adapters.StopAsync(adapterId, dataSourceId.ToString(), drain, cancellationToken);

        _running.Remove(dataSourceId);
    }

    private async Task ReleaseAsync(int dataSourceId)
    {
        if (!_leases.Remove(dataSourceId, out var lease)) return;
        await lease.DisposeAsync();
    }

    /// <summary>
    /// Connection settings from the data source, plus the endpoints its gateways want consumed.
    /// The adapter decides what to do with them — Bitween does not model any broker's topology.
    /// </summary>
    private static Dictionary<string, string> BuildStartupValues(DataSource dataSource,
        IEnumerable<BusGateway> gateways)
    {
        var values = new Dictionary<string, string>(dataSource.Properties ?? new Dictionary<string, string>(),
            StringComparer.OrdinalIgnoreCase);

        var mine = gateways.Where(g => g.DataSourceId == dataSource.Id).ToList();

        var wanted = mine.Where(g => !string.IsNullOrWhiteSpace(g.Endpoint))
            .Select(g => g.Endpoint)
            .Distinct()
            .ToList();

        if (wanted.Count > 0) values["Endpoints"] = string.Join(",", wanted);

        // Per-gateway overrides, namespaced so they cannot collide with connection settings.
        // See BusGateway.EndpointProperties: no bundled adapter reads these yet. Skipping the
        // catch-all gateway is deliberate — an override with no endpoint to attach to would land
        // under the meaningless key "Endpoint::something".
        foreach (var gateway in mine.Where(g => !string.IsNullOrWhiteSpace(g.Endpoint)))
            foreach (var kv in gateway.EndpointProperties ?? new())
                values[$"Endpoint:{gateway.Endpoint}:{kv.Key}"] = kv.Value;

        return values;
    }

    private static string Fingerprint(DataSource dataSource, Dictionary<string, string> startupValues) =>
        dataSource.AdapterId + "|" +
        string.Join(";", startupValues.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));

    /// <summary>
    /// Health from the heartbeat, written back so the UI and the notifiers can see a broker that
    /// has gone away without anyone tailing logs.
    /// </summary>
    private async Task WriteBackHealthAsync(BitweenDbContext dbContext, CancellationToken cancellationToken)
    {
        var health = _adapters.Describe().ToDictionary(h => h.InstanceKey);
        if (health.Count == 0) return;

        var ids = health.Keys.Select(k => int.TryParse(k, out var id) ? id : 0).Where(i => i > 0).ToList();
        var rows = await dbContext.Set<DataSource>()
            .Where(d => ids.Contains(d.Id))
            .ToListAsync(cancellationToken);

        var changed = false;

        foreach (var row in rows)
        {
            if (!health.TryGetValue(row.Id.ToString(), out var instance)) continue;

            row.LastKnownState = instance.ReportedState ?? instance.State.ToString();
            row.LastHeartbeatOn = instance.LastHeartbeatOn?.UtcDateTime;
            row.LastException = instance.LastError;
            row.ConsecutiveFailures = instance.RestartCount;
            row.OwnedByNode = _leases.TryGetValue(row.Id, out var lease)
                ? $"{(_election as RabbitMqLeaderElection)?.NodeName ?? Environment.MachineName} (term {lease.Term})"
                : null;
            changed = true;
        }

        if (changed) await dbContext.SaveChangesAsync(cancellationToken);
    }
}
