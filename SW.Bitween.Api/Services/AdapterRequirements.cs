using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SW.Bitween.Services.Adapters;
using SW.PrimitiveTypes;

namespace SW.Bitween;

/// <summary>
/// Which of an adapter's required startup properties a caller failed to supply.
/// <para>
/// Asking this question means knowing whether the adapter runs in-process or in a serverless
/// container, and the answer was written out four times across the subscription validators
/// before this existed — three in Update alone. Create needs the same answer, and six copies
/// of it would be six places for the rule to drift.
/// </para>
/// </summary>
public class AdapterRequirements(
    NativeAdapterDiscoveryService nativeAdapterDiscovery,
    IServiceProvider serviceProvider)
{
    /// <param name="adapterId">Native (<c>native:</c> prefix) or serverless. Null/blank means nothing is missing.</param>
    /// <param name="provided">What the caller supplied. Blank values count as not supplied.</param>
    public async Task<IReadOnlyCollection<string>> MissingFor(string adapterId, ICollection<KeyAndValue> provided)
    {
        if (string.IsNullOrEmpty(adapterId)) return Array.Empty<string>();

        IEnumerable<string> required;
        if (adapterId.StartsWith(NativeAdapterDiscoveryService.NativePrefix, StringComparison.OrdinalIgnoreCase))
        {
            required = nativeAdapterDiscovery.GetStartupValues(adapterId)
                .Where(p => !p.Value.Optional).Select(p => p.Key);
        }
        else if (await ResidentAdapters.IsResidentAsync(serviceProvider, adapterId))
        {
            // A resident adapter cannot answer this. The probe below spawns it down the CLASSIC
            // stdio path, and a resident one dials out instead of speaking stdio — so the call
            // waits for an answer that never comes and fails as "Received null data", which
            // stopped any subscription naming one from being saved at all.
            //
            // Nothing is missing, and that is the right answer rather than a shrug: a resident
            // adapter's connection settings live on its DATA SOURCE, where the Test button
            // validates them against the real system. The subscription supplies none of them.
            return Array.Empty<string>();
        }
        else
        {
            // Resolved late: starting a serverless adapter is expensive, and most validations
            // never reach this branch.
            var serverless = serviceProvider.GetRequiredService<IServerlessService>();
            await serverless.StartAsync(adapterId, null);
            required = (await serverless.GetExpectedStartupValues())
                .Where(p => p.Value.Optional == false).Select(p => p.Key);
        }

        return required
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            .Except((provided ?? Array.Empty<KeyAndValue>())
                .Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => p.Key))
            .ToArray();
    }
}
