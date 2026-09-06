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
        foreach (var kv in properties ?? new Dictionary<string, string>())
            spec.StartupValues[kv.Key] = kv.Value;

        // Rented, not started: the process is already up, so the call costs a round trip rather
        // than a launch. Returning the lease is what releases it to the next message — and what
        // triggers the reset that clears per-message state between borrowers.
        return new ResidentAdapterSession(await adapters.RentAsync(spec));
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
