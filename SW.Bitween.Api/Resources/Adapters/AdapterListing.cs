using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SW.Bitween.Domain;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Adapters;

/// <summary>One adapter of a kind, and the versions of it that are published.</summary>
/// <param name="Key">The adapter id, as a subscription stores it.</param>
/// <param name="Native">In-process, so it has no published versions of its own.</param>
/// <param name="VersionPaths">
/// Published version files, as paths relative to the remote adapter root — the trailing segment is
/// the version number.
/// </param>
public record AdapterEntry(string Key, bool Native, IReadOnlyList<string> VersionPaths);

/// <summary>
/// The list of adapters of one kind: the in-process ones plus whatever is published to storage.
/// </summary>
/// <remarks>
/// Shared by <see cref="SearchVersioned"/>, which answers with the shape the older UI reads, and
/// <see cref="Catalog"/>, which answers with the same list plus each adapter's startup properties.
/// The grouping of version files under their adapter is fiddly enough that a second copy of it
/// would be a second thing to get wrong.
/// </remarks>
public class AdapterListing(
    ServerlessOptions serverlessOptions,
    ICloudFilesService cloudFilesService,
    NativeAdapterDiscoveryService nativeAdapterDiscovery,
    BitweenDbContext dbContext)
{
    /// <param name="prefix">The plural, lowercase kind: <c>receivers</c>, <c>handlers</c>, …</param>
    /// <returns>Native adapters first, then the published ones.</returns>
    public async Task<List<AdapterEntry>> List(string prefix)
    {
        var index = serverlessOptions.AdapterRemotePath.Length + 1;

        var native = (await nativeAdapterDiscovery.GetNativeAdapters(prefix).ExceptRetiring(dbContext))
            .Select(key => new AdapterEntry(key, true, []))
            .ToList();

        var files = (await cloudFilesService.ListAsync($"{serverlessOptions.AdapterRemotePath}/infolink6.{prefix}"))
            .Where(item => item.Size > 0)
            .ToList();

        var published = files
            .GroupBy(i =>
            {
                var lastSection = i.Key.Split("/").Last();
                var isSemver = Semver.IsVersionNumber(lastSection);
                return isSemver ? i.Key.Split("/").ElementAt(^2) : lastSection;
            })
            .Select(g => new AdapterEntry(
                g.Key,
                false,
                g.Where(v => v.Key != g.Key && Semver.IsVersionNumber(v.Key.Split("/").Last()))
                    .Select(v => v.Key[index..])
                    .ToList()));

        return native.Concat(published).ToList();
    }
}
