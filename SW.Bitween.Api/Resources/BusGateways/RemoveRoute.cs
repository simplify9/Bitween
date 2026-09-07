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
        private readonly BitweenDbContext _dbContext = dbContext;
        private readonly RequestContext _requestContext = requestContext;
        private readonly IInfolinkCache _cache = cache;

        public async Task<object> Handle(int gatewayId, RemoveRouteRequest request)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.BusGateways.Edit);

            var route = await _dbContext.Set<BusGatewayRoute>()
                .FirstOrDefaultAsync(r => r.Id == request.RouteId && r.BusGatewayId == gatewayId);

            if (route == null)
                throw new SWNotFoundException($"Route with Id {request.RouteId} not found in gateway {gatewayId}");

            _dbContext.Remove(route);
            await _dbContext.SaveChangesAsync();
            await _cache.BroadcastRevoke();
            return null;
        }
    }
}
