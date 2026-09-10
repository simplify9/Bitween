using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SW.PrimitiveTypes;

namespace SW.Bitween;

/// <summary>
/// What startup properties an adapter expects — its key names, which are optional, which are
/// secret, and their defaults.
/// </summary>
/// <remarks>
/// <para>
/// Answering this means knowing whether the adapter runs in-process or in a serverless container,
/// and that fork was written out five times across the adapter and subscription resources before
/// this existed. Worse, the serverless half is expensive out of all proportion to what it returns:
/// it downloads and unzips the adapter package, spawns a <c>dotnet</c> child process, and talks to
/// it over stdio, all to be told a handful of key names. A screen that lists the adapter catalogue
/// pays that once per adapter.
/// </para>
/// <para>
/// So the answer is cached. It is a schema, not data: nothing a user does in the UI can change it,
/// because a subscription's actual property <em>values</em> live in the database and are never part
/// of this. It changes only when a new adapter package is uploaded, and the cache is held for
/// <see cref="ServerlessOptions.AdapterMetadataCacheDuration"/> — the same window in which
/// <c>ServerlessService</c> already serves a stale <c>Hash</c> from its own metadata cache and so
/// boots the previous build regardless. Caching here therefore adds no staleness that re-uploading
/// an adapter did not already have.
/// </para>
/// </remarks>
public class AdapterStartupValues(
    NativeAdapterDiscoveryService nativeAdapterDiscovery,
    IServiceScopeFactory scopeFactory,
    IMemoryCache memoryCache,
    ServerlessOptions serverlessOptions)
{
    private const string CacheKeyPrefix = "bitween.adapters.startupvalues";

    /// <param name="adapterId">Native (<c>native</c> prefix) or serverless.</param>
    /// <returns>Key name to description. Empty when the adapter reports nothing.</returns>
    public async Task<IDictionary<string, StartupValue>> Describe(string adapterId)
    {
        if (string.IsNullOrWhiteSpace(adapterId))
            return new Dictionary<string, StartupValue>();

        // Reflection over an in-process type, so there is nothing here worth caching.
        if (adapterId.StartsWith(NativeAdapterDiscoveryService.NativePrefix, StringComparison.OrdinalIgnoreCase))
            return nativeAdapterDiscovery.GetStartupValues(adapterId);

        var cacheKey = $"{CacheKeyPrefix}.{adapterId.ToLowerInvariant()}";
        if (memoryCache.TryGetValue(cacheKey, out IDictionary<string, StartupValue> cached))
            return cached;

        // Its own scope, disposed the moment this returns, because disposing the serverless
        // service is what quits the child process. Left on the request's scope instead, a caller
        // describing a whole catalogue would hold every adapter it started open until the request
        // ended, rather than one at a time.
        await using var scope = scopeFactory.CreateAsyncScope();
        var serverless = scope.ServiceProvider.GetRequiredService<IServerlessService>();

        try
        {
            await serverless.StartAsync(adapterId, null);
        }
        catch (KeyNotFoundException ex)
        {
            throw new BitweenException(
                $"Adapter '{adapterId}' metadata is incomplete or the adapter package is not installed. " +
                $"Missing metadata key: {ex.Message}", ex);
        }

        var startupValues = await serverless.GetExpectedStartupValues() ?? new Dictionary<string, StartupValue>();

        // Read-only because one instance is now handed to every request that asks. A caller that
        // edited it in place would be editing what the next request is told the adapter expects,
        // and this way that attempt throws instead.
        IDictionary<string, StartupValue> shared =
            new ReadOnlyDictionary<string, StartupValue>(startupValues);

        // Only a successful answer is cached — a boot that failed because, say, the adapter's
        // runtime is missing locally must not stick around as "this adapter has no properties".
        return memoryCache.Set(cacheKey, shared,
            TimeSpan.FromMinutes(serverlessOptions.AdapterMetadataCacheDuration));
    }
}
