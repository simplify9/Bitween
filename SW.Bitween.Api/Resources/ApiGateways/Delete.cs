using Microsoft.EntityFrameworkCore;
using SW.EfCoreExtensions;
using SW.Bitween.Domain.Gateway;
using SW.PrimitiveTypes;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.ApiGateways
{
    public class Delete(BitweenDbContext dbContext, RequestContext requestContext) : IDeleteHandler<int, object>
    {
        public async Task<object> Handle(int key)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.ApiGateways.Delete);

            var gateway = await dbContext.Set<ApiGateway>()
                .Include(ag => ag.Partners)
                .FirstOrDefaultAsync(ag => ag.Id == key);

            if (gateway == null)
                throw new SWNotFoundException($"ApiGateway with Id {key} not found");

            // Partners are FK-restricted to the gateway; remove them explicitly before the gateway.
            if (gateway.Partners != null && gateway.Partners.Count > 0)
                dbContext.RemoveRange(gateway.Partners);

            dbContext.Remove(gateway);
            await dbContext.SaveChangesAsync();
            return null;
        }
    }
}

