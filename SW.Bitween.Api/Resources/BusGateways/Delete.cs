using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.Gateway;
using SW.PrimitiveTypes;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.BusGateways
{
public class Delete(BitweenDbContext dbContext, RequestContext requestContext, IInfolinkCache cache)
        : IDeleteHandler<int, object>
    {
        public async Task<object> Handle(int key)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.BusGateways.Delete);

            var gateway = await dbContext.Set<BusGateway>()
                .Include(bg => bg.Routes)
                .FirstOrDefaultAsync(bg => bg.Id == key);

            if (gateway == null)
                throw new SWNotFoundException($"BusGateway with Id {key} not found");

            // Routes are FK-restricted to the gateway; remove them explicitly before the gateway.
            if (gateway.Routes != null && gateway.Routes.Count > 0)
                dbContext.RemoveRange(gateway.Routes);

            dbContext.Remove(gateway);
            await dbContext.SaveChangesAsync();
            await cache.BroadcastRevoke();
            return null;
        }
    }
}
