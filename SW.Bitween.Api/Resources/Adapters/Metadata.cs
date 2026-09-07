using System;
using System.Linq;
using System.Threading.Tasks;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Adapters;

[HandlerName("Metadata")]
public class Metadata(
    ServerlessOptions serverlessOptions,
    ICloudFilesService cloudFilesService,
    NativeAdapterDiscoveryService nativeAdapterDiscovery,
    BitweenDbContext dbContext,
    RequestContext requestContext
    ) : IGetHandler<string, object>
{
    public async Task<object> Handle(string key)
    {
        await requestContext.EnsurePermission(dbContext, Model.Permissions.Subscriptions.View);

        var decodedKey = Uri.UnescapeDataString(key);

        if (decodedKey.StartsWith(NativeAdapterDiscoveryService.NativePrefix, StringComparison.OrdinalIgnoreCase))
            return new { };

        var cloudFilesList =
            await cloudFilesService.GetMetadataAsync(
                $"{serverlessOptions.AdapterRemotePath}/{decodedKey}"
            );

        return cloudFilesList;
    }
}