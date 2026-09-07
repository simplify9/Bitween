using SW.EfCoreExtensions;
using SW.Bitween.Domain;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.Documents
{
public class Delete(BitweenDbContext dbContext, RequestContext requestContext, IInfolinkCache cache)
        : IDeleteHandler<int,object>
    {
        async public Task<object> Handle(int key)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.Documents.Delete);

            await dbContext.DeleteByKeyAsync<Document>(key);
            await cache.BroadcastRevoke();
            return null;
        }
    }
}