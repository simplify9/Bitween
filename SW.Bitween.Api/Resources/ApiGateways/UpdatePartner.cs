using SW.Bitween.Domain.Gateway;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using SW.Bitween.Domain;

namespace SW.Bitween.Resources.ApiGateways
{
    [HandlerName(nameof(UpdatePartner))]
public class UpdatePartner(BitweenDbContext dbContext, RequestContext requestContext)
        : ICommandHandler<int, ApiGatewayPartnerCreate, object>
    {
        public async Task<object> Handle(int gatewayId, ApiGatewayPartnerCreate model)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.ApiGateways.Edit);

            var gateway = await dbContext.Set<ApiGateway>()
                .Include(ag => ag.Partners)
                .FirstOrDefaultAsync(ag => ag.Id == gatewayId);

            if (gateway == null)
                throw new SWNotFoundException($"ApiGateway with Id {gatewayId} not found");

            // Validate subscription exists and is of type GatewayApiCall
            // Repointing an existing attachment always names an integration that already
            // exists; defining one inline is only for the attachment being created.
            if (!model.SubscriptionId.HasValue)
                throw new SWValidationException(GatewayLinkTarget.NeitherGiven,
                    "Pick the integration this partner runs.");

            var subscription = await dbContext.Set<Subscription>()
                .FirstOrDefaultAsync(s => s.Id == model.SubscriptionId);

            if (subscription == null)
                throw new SWNotFoundException($"Subscription with Id {model.SubscriptionId} not found");

            if (subscription.Type != SubscriptionType.GatewayApiCall)
                throw new SWException($"Subscription must be of type GatewayApiCall. Current type: {subscription.Type}");

            var partnerLink = gateway.Partners?
                .FirstOrDefault(p => p.PartnerId == model.PartnerId);

            if (partnerLink == null)
                throw new SWNotFoundException($"Partner with Id {model.PartnerId} not found in gateway {gatewayId}");

            partnerLink.SubscriptionId = model.SubscriptionId.Value;

            await dbContext.SaveChangesAsync();

            return null;
        }
    }
}

