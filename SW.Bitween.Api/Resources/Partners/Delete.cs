using SW.EfCoreExtensions;
using SW.Bitween.Domain;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.Partners
{
    public class Delete(BitweenDbContext dbContext, RequestContext requestContext) : IDeleteHandler<int,object>
    {
        public async Task<object> Handle(int key)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.Partners.Delete);

            if (key == Partner.SystemId)
                throw new SWException("System partner can not be deleted.");

            await dbContext.DeleteByKeyAsync<Partner>(key);
            return null;
        }
    }
}