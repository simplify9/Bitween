using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Domain.Gateway;
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
/// PLACEMENT IS NOT DONE HERE YET. A broker connection is exclusive, so exactly one node may hold
/// it; today every node would try. Until leader election lands, run this on a single instance or
/// leave <see cref="BitweenOptions.BusProvidersEnabled"/> off. The DataSource.OwnedByNode column
/// exists for that election to write into.
/// </summary>
public class BusProviderSupervisor : BackgroundService
{
    private static readonly TimeSpan ReconcileInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly IResidentAdapterHost _adapters;
    private readonly ILogger<BusProviderSupervisor> _logger;

    // What we last started, and the configuration fingerprint it was started with.
    private readonly Dictionary<int, string> _running = new();

    public BusProviderSupervisor(IServiceProvider serviceProvider, IResidentAdapterHost adapters,
        ILogger<BusProviderSupervisor> logger)
    {
        _serviceProvider = serviceProvider;
        _adapters = adapters;
        _logger = logger;
    }

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

    private async Task ReconcileAsync(CancellationToken cancellationToken)
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

        foreach (var dataSource in desired)
        {
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

        // Anything running that is no longer desired.
        var wanted = desired.Select(d => d.Id).ToHashSet();
        foreach (var id in _running.Keys.Where(k => !wanted.Contains(k)).ToList())
        {
            var adapterId = desired.FirstOrDefault(d => d.Id == id)?.AdapterId;
            if (adapterId != null)
                await _adapters.StopAsync(adapterId, id.ToString(), drain: true, cancellationToken);
            _running.Remove(id);
        }

        await WriteBackHealthAsync(dbContext, cancellationToken);
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
        foreach (var gateway in mine)
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
            row.OwnedByNode = Environment.MachineName;
            changed = true;
        }

        if (changed) await dbContext.SaveChangesAsync(cancellationToken);
    }
}
