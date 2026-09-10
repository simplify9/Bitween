using SW.Bitween.Domain.Gateway;
using SW.PrimitiveTypes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace SW.Bitween.Resources.ApiGateways
{
    [HandlerName(nameof(RemovePartner))]
public class RemovePartner(BitweenDbContext dbContext, RequestContext requestContext)
        : ICommandHandler<int, RemovePartnerRequest, object>
    {
        public async Task<object> Handle(int gatewayId, RemovePartnerRequest request)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.ApiGateways.Edit);

            var gateway = await dbContext.Set<ApiGateway>()
                .Include(ag => ag.Partners)
                .FirstOrDefaultAsync(ag => ag.Id == gatewayId);

            if (gateway == null)
                throw new SWNotFoundException($"ApiGateway with Id {gatewayId} not found");

            var partnerLink = gateway.Partners?
                .FirstOrDefault(p => p.PartnerId == request.PartnerId);

            if (partnerLink == null)
                throw new SWNotFoundException($"Partner with Id {request.PartnerId} not found in gateway {gatewayId}");

            dbContext.Remove(partnerLink);
            await dbContext.SaveChangesAsync();

            return null;
        }
    }

    public class RemovePartnerRequest
    {
        public int PartnerId { get; set; }
    }
}

