using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SW.PrimitiveTypes;

namespace SW.Bitween;

/// <summary>
/// Which of an adapter's required startup properties a caller failed to supply.
/// <para>
/// The answer was written out four times across the subscription validators before this
/// existed — three in Update alone. Create needs the same answer, and six copies of it would
/// be six places for the rule to drift. Whether the adapter runs in-process or in a serverless
/// container is <see cref="AdapterStartupValues"/>'s problem, not this one's.
/// </para>
/// </summary>
public class AdapterRequirements(AdapterStartupValues startupValues)
{
    /// <param name="adapterId">Native (<c>native:</c> prefix) or serverless. Null/blank means nothing is missing.</param>
    /// <param name="provided">What the caller supplied. Blank values count as not supplied.</param>
    public async Task<IReadOnlyCollection<string>> MissingFor(string adapterId, ICollection<KeyAndValue> provided)
    {
        if (string.IsNullOrEmpty(adapterId)) return Array.Empty<string>();

        var required = (await startupValues.Describe(adapterId))
            .Where(p => !p.Value.Optional).Select(p => p.Key);

        return required
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            .Except((provided ?? Array.Empty<KeyAndValue>())
                .Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => p.Key))
            .ToArray();
    }
}
