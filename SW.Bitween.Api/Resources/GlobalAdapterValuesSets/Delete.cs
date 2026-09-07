using System.Threading.Tasks;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.GlobalAdapterValuesSets
{
    [HandlerName("delete")]
public class Delete(BitweenDbContext dbContext, RequestContext requestContext, IInfolinkCache cache)
        : ICommandHandler<string, DeleteGlobalAdapterValuesSetModel, object>
    {
        public async Task<object> Handle(string key, DeleteGlobalAdapterValuesSetModel _)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.GlobalValues.Delete);

            var entity = await dbContext.Set<GlobalAdapterValuesSet>().FindAsync(key);
            if (entity is null)
                throw new SWValidationException("NOT_FOUND", $"GlobalAdapterValuesSet with id {key} was not found");

            dbContext.Remove(entity);
            await dbContext.SaveChangesAsync();
            await cache.BroadcastRevoke();
            return null;
        }
    }
}
