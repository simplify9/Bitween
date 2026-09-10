using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Adapters;

/// <summary>
/// Every adapter of one kind, each with the startup properties it expects.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchVersioned"/> answers with the adapters alone, which left a caller that needs to
/// draw a form per adapter to ask <see cref="GetStartupValues"/> once per row. On a screen listing
/// all four kinds that is around ninety requests, six of which a browser will run at a time, so the
/// last of them waits behind fifteen rounds of queueing — and every one of those requests carries
/// its own permission check. Answering the whole kind at once makes it four requests for the screen.
/// </para>
/// <para>
/// <see cref="SearchVersioned"/> is deliberately left as it is: the older UI reads it, does not need
/// the properties, and should not start paying for them.
/// </para>
/// </remarks>
[HandlerName("Catalog")]
public class Catalog(
    AdapterListing listing,
    AdapterStartupValues startupValues,
    BitweenDbContext dbContext,
    RequestContext requestContext) : IQueryHandler<AdapterSearchRequest, object>
{
    /// <summary>
    /// How many adapters are described at once when none of them are cached yet.
    /// <para>
    /// Describing a published adapter starts a child process, so this is a limit on how many of
    /// those exist at the same moment. Six is what a browser was already doing per host, so a cold
    /// catalogue is no slower than it used to be without being any heavier on the server.
    /// </para>
    /// </summary>
    private const int DescribeConcurrency = 6;

    public async Task<object> Handle(AdapterSearchRequest request)
    {
        await requestContext.EnsurePermission(dbContext, Model.Permissions.Subscriptions.View);

        var adapters = await listing.List(request.Prefix);

        var described = new ConcurrentDictionary<string, IDictionary<string, StartupValue>>();

        await Parallel.ForEachAsync(
            adapters,
            new ParallelOptions { MaxDegreeOfParallelism = DescribeConcurrency },
            async (adapter, _) =>
            {
                try
                {
                    described[adapter.Key] = await startupValues.Describe(adapter.Key);
                }
                catch (Exception)
                {
                    // One adapter that cannot be described — its runtime is missing locally, say —
                    // must not blank out the rest of the catalogue, including the native ones that
                    // resolved perfectly well. It comes back with no properties, as it did when the
                    // caller was asking row by row and swallowing the failure itself.
                    described[adapter.Key] = new Dictionary<string, StartupValue>();
                }
            });

        return adapters.Select(a => new
        {
            a.Key,
            a.Native,
            // Just the version numbers. VersionPaths carries each one as a path, which is what the
            // older shape passed through and what made a version read as an object rather than
            // "1.2.3" to anything trying to label it.
            Versions = a.VersionPaths.Select(v => v.Split('/').Last()).ToList(),
            StartupValues = described.GetValueOrDefault(a.Key) ?? new Dictionary<string, StartupValue>()
        });
    }
}
