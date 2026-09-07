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
        private readonly BitweenDbContext _dbContext = dbContext;
        private readonly RequestContext _requestContext = requestContext;
        private readonly IInfolinkCache _cache = cache;

        public async Task<object> Handle(string key, DeleteGlobalAdapterValuesSetModel _)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.GlobalValues.Delete);

            var entity = await _dbContext.Set<GlobalAdapterValuesSet>().FindAsync(key);
            if (entity is null)
                throw new SWValidationException("NOT_FOUND", $"GlobalAdapterValuesSet with id {key} was not found");

            _dbContext.Remove(entity);
            await _dbContext.SaveChangesAsync();
            await _cache.BroadcastRevoke();
            return null;
        }
    }
}
