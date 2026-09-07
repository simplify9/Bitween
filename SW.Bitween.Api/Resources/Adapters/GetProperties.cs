using SW.Bitween.Domain;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.Adapters
{
    [HandlerName("properties")]
    public class GetProperties(IServerlessService serverless, NativeAdapterDiscoveryService nativeAdapterDiscovery,
        BitweenDbContext dbContext, RequestContext requestContext) : IGetHandler<string,object>
    {
        private readonly IServerlessService serverless = serverless;
        private readonly BitweenDbContext dbContext = dbContext;
        private readonly RequestContext requestContext = requestContext;

        async public Task<object> Handle(string key)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.Subscriptions.View);

            var decodedKey = Uri.UnescapeDataString(key);
            
            // Check if it's a native adapter
            if (decodedKey.StartsWith(NativeAdapterDiscoveryService.NativePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return nativeAdapterDiscovery.GetExpectedStartupValues(decodedKey);
            }
            
            // Handle serverless adapters
            try
            {
                await serverless.StartAsync(decodedKey, null);
            }
            catch (KeyNotFoundException ex)
            {
                throw new BitweenException(
                    $"Adapter '{decodedKey}' metadata is incomplete or the adapter package is not installed. " +
                    $"Missing metadata key: {ex.Message}", ex);
            }

            var expected = await serverless.GetExpectedStartupValues();
            if (expected == null)
                return new Dictionary<string, string>();

            return expected
                .ToList()
                .ToDictionary(
                    k => k.Key,
                    v => $"{v.Key} {(v.Value.Optional ? $" ({v.Value.Default ?? "null"})" : " *")}");
        }
    }

}
