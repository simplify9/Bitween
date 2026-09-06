using System;
using System.Collections.Generic;
using SW.PrimitiveTypes;
using System.Threading.Tasks;
using System.Linq;
using SW.Bitween.Model;

namespace SW.Bitween.Resources.Adapters
{
    [HandlerName("Versioned")]
    public class SearchVersioned : IQueryHandler<AdapterSearchRequest,object>
    {
        private readonly ServerlessOptions _serverlessOptions;
        private readonly ICloudFilesService _cloudFilesService;
        private readonly NativeAdapterDiscoveryService _nativeAdapterDiscovery;
        private readonly BitweenDbContext _dbContext;
        private readonly RequestContext _requestContext;
        private readonly SW.Serverless.AdapterInstaller _adapterInstaller;

        public SearchVersioned(ServerlessOptions serverlessOptions, ICloudFilesService cloudFilesService,
            NativeAdapterDiscoveryService nativeAdapterDiscovery, BitweenDbContext dbContext,
            RequestContext requestContext, SW.Serverless.AdapterInstaller adapterInstaller)
        {
            _adapterInstaller = adapterInstaller;
            _serverlessOptions = serverlessOptions;
            _cloudFilesService = cloudFilesService;
            _nativeAdapterDiscovery = nativeAdapterDiscovery;
            _dbContext = dbContext;
            _requestContext = requestContext;
        }


        public async Task<object> Handle(AdapterSearchRequest request)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.Subscriptions.View);

            var index = _serverlessOptions.AdapterRemotePath.Length + 1;

            // Get native adapters first (they don't have versions)
            var nativeAdapters = _nativeAdapterDiscovery.GetNativeAdapters(request.Prefix)
                .Select(key => new
                {
                    Key = key,
                    Versions = new List<object>() // Native adapters have no versions
                })
                .ToList();

            // Two ways an adapter says what it is for, and both are honoured.
            //
            // The Kind stamped on it at publish time is the real answer: it comes from the code
            // rather than from whoever typed the id, and it lets an adapter be reclassified without
            // being renamed — a rename is not free, because every subscription stores the id.
            //
            // The infolink6.<kind>s. prefix is the old convention, and everything published before
            // the stamp exists carries nothing else. Dropping it would empty this list on every
            // deployment that has not republished, so it stays as the fallback.
            var cloudFilesList = (await ListByKindAsync(request.Prefix))
                .Where(item => item.Size > 0)
                .ToList();

            var grouped = cloudFilesList
                .GroupBy(i =>
                {
                    var lastSection = i.Key.Split("/").Last();
                    var isSemver = Semver.IsVersionNumber(lastSection);
                    var key = isSemver ? i.Key.Split("/").ElementAt(^2) : lastSection;

                    return key;
                });

            var externalAdapters = grouped.Select(i => new
            {
                i.Key,
                Versions = i.Where(v => v.Key != i.Key && Semver.IsVersionNumber(v.Key.Split("/").Last()))
                    .Select(v => new
                    {
                        Key = v.Key[index..]
                    }).ToList()
            });

            // Return native adapters first, then external
            return nativeAdapters.Concat<object>(externalAdapters);
        }

        /// <summary>
        /// Everything published under the old naming convention for this kind, plus everything
        /// that declared the kind in its metadata regardless of what it is called.
        /// </summary>
        private async Task<List<CloudFileInfo>> ListByKindAsync(string prefix)
        {
            var root = _serverlessOptions.AdapterRemotePath;

            var byConvention = (await _cloudFilesService.ListAsync($"{root}/infolink6.{prefix}")).ToList();
            var named = byConvention.Select(i => i.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // The plural the UI asks with — "handlers" — against the singular an adapter declares.
            var kind = prefix?.TrimEnd('s') ?? "";
            if (string.IsNullOrWhiteSpace(kind)) return byConvention;

            foreach (var item in await _cloudFilesService.ListAsync($"{root}/"))
            {
                if (item.Size <= 0 || named.Contains(item.Key)) continue;

                var declared = await DeclaredKindsAsync(item.Key, root);
                if (declared.Contains(kind, StringComparer.OrdinalIgnoreCase))
                    byConvention.Add(item);
            }

            return byConvention;
        }

        /// <summary>
        /// The kinds one adapter declared. Metadata reads are cached by the installer, and an
        /// adapter whose metadata cannot be read simply declares nothing rather than taking the
        /// whole catalog down with it — the list is what an operator needs to configure anything
        /// at all.
        /// </summary>
        private async Task<string[]> DeclaredKindsAsync(string key, string root)
        {
            try
            {
                var adapterId = key.StartsWith($"{root}/", StringComparison.OrdinalIgnoreCase)
                    ? key[(root.Length + 1)..]
                    : key;

                // Versioned uploads keep the adapter id one segment up from the version.
                if (Semver.IsVersionNumber(adapterId.Split('/').Last()))
                    adapterId = string.Join('/', adapterId.Split('/')[..^1]);

                var metadata = await _adapterInstaller.GetMetadataAsync(adapterId);
                if (metadata?.AdapterValues == null) return [];

                return metadata.AdapterValues.TryGetValue("Kind", out var kinds) && !string.IsNullOrWhiteSpace(kinds)
                    ? kinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : [];
            }
            catch
            {
                return [];
            }
        }
    }
}