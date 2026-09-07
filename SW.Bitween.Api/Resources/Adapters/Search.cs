using System.Collections.Generic;
using SW.PrimitiveTypes;
using System.Threading.Tasks;
using System.Linq;
using SW.Bitween.Model;

namespace SW.Bitween.Resources.Adapters
{
    public class Search(ServerlessOptions serverlessOptions, ICloudFilesService cloudFilesService,
        NativeAdapterDiscoveryService nativeAdapterDiscovery, BitweenDbContext dbContext,
        RequestContext requestContext) : IQueryHandler<AdapterSearchRequest,object>
    {
        public async Task<object> Handle(AdapterSearchRequest request)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.Subscriptions.View);

            // Get native adapters first
            var nativeAdapters = nativeAdapterDiscovery.GetNativeAdapters(request.Prefix).ToList();

            // Get external adapters from storage
            var cloudFilesList =
                (await cloudFilesService.ListAsync(
                    $"{serverlessOptions.AdapterRemotePath}/infolink6.{request.Prefix}"))
                .Where(item => item.Size > 0)
                .Select(i =>
                {
                    var lastSection = i.Key.Split("/").Last();
                    var isSemver = Semver.IsVersionNumber(lastSection);
                    var key = isSemver ? i.Key.Split("/").ElementAt(^2) : lastSection;

                    return key;
                })
                .Distinct()
                .ToList();

            // Combine native (first) and external adapters
            var allAdapters = nativeAdapters.Concat(cloudFilesList);

            return allAdapters.ToDictionary(k => k, v => v);
        }
    }
}