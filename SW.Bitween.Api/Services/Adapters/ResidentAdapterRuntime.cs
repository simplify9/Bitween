using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SW.PrimitiveTypes;
using SW.Serverless;
using SW.Serverless.Resident;

namespace SW.Bitween.Services.Adapters;

/// <summary>
/// A packaged adapter that stays running, rented from the pool and invoked over its socket.
///
/// The two packaged lifecycles are published identically — same zip, same key, same metadata bag —
/// and differ only in their entry point. Running a resident adapter down the classic path does not
/// fail cleanly: the host spawns it and then waits out the command timeout for an answer that
/// never comes, because the adapter dialled out and is waiting for a host that is not listening.
/// So the lifecycle has to be read before deciding how to run it.
/// </summary>
public class ResidentAdapterRuntime(
    IServiceProvider serviceProvider,
    ILogger<ResidentAdapterRuntime> logger) : IAdapterRuntime
{
    public const string LifecycleKey = "Lifecycle";
    public const string ResidentValue = "resident";

    public async Task<bool> CanRunAsync(string adapterId)
    {
        if (string.IsNullOrWhiteSpace(adapterId)) return false;

        try
        {
            // Cached by the installer for AdapterMetadataCacheDuration, so this is not a storage
            // round trip per message.
            var installer = serviceProvider.GetRequiredService<AdapterInstaller>();
            var metadata = await installer.GetMetadataAsync(adapterId);

            return metadata?.AdapterValues != null &&
                   metadata.AdapterValues.TryGetValue(LifecycleKey, out var lifecycle) &&
                   string.Equals(lifecycle, ResidentValue, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // Unreadable metadata means "not mine". The classic runtime picks it up and fails the
            // way it always did, rather than this one inventing a new failure.
            logger.LogDebug(ex, "Could not read the lifecycle of adapter {AdapterId}.", adapterId);
            return false;
        }
    }

    public async Task<IAdapterSession> BeginAsync(string adapterId, AdapterRole role,
        IDictionary<string, string> properties, string correlationId)
    {
        var adapters = serviceProvider.GetService<IResidentAdapterHost>()
            ?? throw new BitweenException(
                $"Adapter '{adapterId}' is a resident adapter, but resident adapters are not "
                + "enabled on this node (Bitween:BusProvidersEnabled). It cannot run here.");

        var spec = new AdapterSpec { AdapterId = adapterId };
        string dataSourceId = null;

        foreach (var kv in properties ?? new Dictionary<string, string>())
        {
            // Reserved and consumed here: it addresses an instance, it is not a setting, and an
            // adapter that saw it would have to know to ignore it.
            if (kv.Key == StartupValuesFiller.DataSourceIdKey) { dataSourceId = kv.Value; continue; }
            spec.StartupValues[kv.Key] = kv.Value;
        }

        // A subscription bound to a data source runs against THAT connection — the instance the
        // supervisor already keeps up for it, holding the pool the data source exists to provide.
        // Renting instead would start a second process with a second pool, configured from
        // subscription properties that do not hold the credentials at all.
        if (dataSourceId != null)
        {
            var running = adapters.Get(adapterId, dataSourceId);
            if (running == null)
                throw new BitweenException(
                    $"Data source {dataSourceId} is not running on this node, so adapter "
                    + $"'{adapterId}' has no connection to work through. If the data source is "
                    + "exclusive, another node holds it; if it is per-node, look at its health — "
                    + "the supervisor could not start it here.");

            // The subscription's own adapter properties travel with each CALL, not with the
            // process: this instance is shared by every subscription bound to the data source, and
            // its startup values are the data source's. Without this a subscription could not say
            // which statement to run — it would be reading whatever the data source was started
            // with, which is the same answer for all of them.
            return new RunningInstanceSession(running, spec.StartupValues);
        }

        // Rented, not started: the process is already up, so the call costs a round trip rather
        // than a launch. Returning the lease is what releases it to the next message — and what
        // triggers the reset that clears per-message state between borrowers.
        return new ResidentAdapterSession(await adapters.RentAsync(spec));
    }

    /// <summary>
    /// A session against the data source's long-lived instance. Nothing is returned on dispose:
    /// the instance is not ours to give back, and it must outlive this Xchange to be any use to
    /// the next one. That also means no per-session reset, so a data source adapter must not keep
    /// per-message state in a field — which is the same rule any shared connection follows.
    /// </summary>
    private sealed class RunningInstanceSession(
        ResidentAdapterInstance instance, IDictionary<string, string> properties) : IAdapterSession
    {
        public Task<TResult> InvokeAsync<TResult>(string method, object argument = null) =>
            instance.InvokeAsync<TResult>(method, argument, properties: properties);

        public Task InvokeAsync(string method, object argument = null) =>
            instance.InvokeAsync<object>(method, argument, properties: properties);

        public ValueTask DisposeAsync() => default;
    }

    private sealed class ResidentAdapterSession(IAdapterLease lease) : IAdapterSession
    {
        public Task<TResult> InvokeAsync<TResult>(string method, object argument = null) =>
            lease.InvokeAsync<TResult>(method, argument);

        public Task InvokeAsync(string method, object argument = null) =>
            lease.InvokeAsync<object>(method, argument);

        public ValueTask DisposeAsync() => lease.DisposeAsync();
    }
}
