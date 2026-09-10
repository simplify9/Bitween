using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SW.PrimitiveTypes;

namespace SW.Bitween;

/// <summary>
/// What startup properties an adapter expects — its key names, which are optional, which are
/// secret, and their defaults.
/// </summary>
/// <remarks>
/// <para>
/// Answering this means knowing whether the adapter runs in-process or is published to storage and
/// run in a child process, and that fork was written out six times across the adapter and
/// subscription resources before this existed.
/// </para>
/// <para>
/// It is a schema, not data: nothing a user does in the UI can change it, because a subscription's
/// actual property <em>values</em> live in the database and are never part of this. It changes only
/// when a new adapter package is uploaded. <see cref="ServerlessAdapterDescriber"/> is what makes
/// use of that.
/// </para>
/// </remarks>
public class AdapterStartupValues(
    NativeAdapterDiscoveryService nativeAdapterDiscovery,
    ServerlessAdapterDescriber serverlessDescriber)
{
    /// <summary>Drops what is remembered about a published adapter.</summary>
    public void Forget(string adapterId) => serverlessDescriber.Forget(adapterId);

    /// <param name="adapterId">Native (<c>native</c> prefix) or published.</param>
    /// <returns>Key name to description. Empty when the adapter reports nothing.</returns>
    public async Task<IDictionary<string, StartupValue>> Describe(string adapterId)
    {
        if (string.IsNullOrWhiteSpace(adapterId))
            return new Dictionary<string, StartupValue>();

        // Reflection over an in-process type, so there is nothing here worth caching, and nothing
        // worth queueing behind the published adapters either.
        if (adapterId.StartsWith(NativeAdapterDiscoveryService.NativePrefix, StringComparison.OrdinalIgnoreCase))
            return nativeAdapterDiscovery.GetStartupValues(adapterId);

        return await serverlessDescriber.Describe(adapterId);
    }
}
