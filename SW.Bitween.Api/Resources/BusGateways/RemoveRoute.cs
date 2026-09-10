using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.Gateway;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.BusGateways
{
    [HandlerName(nameof(RemoveRoute))]
public class RemoveRoute(BitweenDbContext dbContext, RequestContext requestContext, IInfolinkCache cache)
        : ICommandHandler<int, RemoveRouteRequest, object>
    {
        public async Task<object> Handle(int gatewayId, RemoveRouteRequest request)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.BusGateways.Edit);

            var route = await dbContext.Set<BusGatewayRoute>()
                .FirstOrDefaultAsync(r => r.Id == request.RouteId && r.BusGatewayId == gatewayId);

            if (route == null)
                throw new SWNotFoundException($"Route with Id {request.RouteId} not found in gateway {gatewayId}");

            dbContext.Remove(route);
            await dbContext.SaveChangesAsync();
            await cache.BroadcastRevoke();
            return null;
        }
    }
}
