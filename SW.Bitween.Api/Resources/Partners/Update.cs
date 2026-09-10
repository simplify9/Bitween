using SW.EfCoreExtensions;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.Partners
{
public class Update(BitweenDbContext dbContext, RequestContext requestContext)
        : ICommandHandler<int, PartnerUpdate,object>
    {
        public async Task<object> Handle(int key, PartnerUpdate model)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.Partners.Edit);

            var entity = await dbContext.FindAsync<Partner>(key);
            entity.SetApiCredentials(model.ApiCredentials.Select(kv => new ApiCredential(kv.Key, kv.Value)));
            entity.AdapterProperties = model.AdapterProperties;
            dbContext.Entry(entity).SetProperties(model);
            await dbContext.SaveChangesAsync();
            return null;
        }
    }
}