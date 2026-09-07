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
    private readonly ServerlessOptions _serverlessOptions = serverlessOptions;
    private readonly ICloudFilesService _cloudFilesService = cloudFilesService;
    private readonly NativeAdapterDiscoveryService _nativeAdapterDiscovery = nativeAdapterDiscovery;
    private readonly BitweenDbContext _dbContext = dbContext;
    private readonly RequestContext _requestContext = requestContext;

    public async Task<object> Handle(string key)
    {
        await _requestContext.EnsurePermission(_dbContext, Model.Permissions.Subscriptions.View);

        var decodedKey = Uri.UnescapeDataString(key);

        if (decodedKey.StartsWith(NativeAdapterDiscoveryService.NativePrefix, StringComparison.OrdinalIgnoreCase))
            return new { };

        var cloudFilesList =
            await _cloudFilesService.GetMetadataAsync(
                $"{_serverlessOptions.AdapterRemotePath}/{decodedKey}"
            );

        return cloudFilesList;
    }
}