using SW.EfCoreExtensions;
using SW.Bitween.Domain.Gateway;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SW.Bitween.Resources.ApiGateways
{
public class Update(BitweenDbContext dbContext, RequestContext requestContext)
        : ICommandHandler<int, ApiGatewayUpdate, object>
    {
        private readonly BitweenDbContext _dbContext = dbContext;
        private readonly RequestContext _requestContext = requestContext;

        public async Task<object> Handle(int key, ApiGatewayUpdate model)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.ApiGateways.Edit);

            var entity = await _dbContext.Set<ApiGateway>()
                .Include(ag => ag.Partners)
                .FirstOrDefaultAsync(ag => ag.Id == key);

            if (entity == null)
                throw new SWNotFoundException($"ApiGateway with Id {key} not found");

            GatewayUrlName.Validate(model.UrlName);

            entity.Name = model.Name;
            entity.UrlName = model.UrlName;
            entity.Inactive = model.Inactive;

            await _dbContext.SaveChangesAsync();
            return null;
        }
    }
}

