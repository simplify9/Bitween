using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.GlobalAdapterValuesSets
{
    public class Get(BitweenDbContext dbContext, RequestContext requestContext) : IGetHandler<string, object>
    {
        private readonly BitweenDbContext _dbContext = dbContext;
        private readonly RequestContext _requestContext = requestContext;

        public async Task<object> Handle(string key)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.GlobalValues.View);

            var entity = await _dbContext.Set<GlobalAdapterValuesSet>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == key);
            
            if (entity == null)
                throw new SWNotFoundException($"GlobalAdapterValuesSet with id '{key}' was not found");

            return new GlobalAdapterValuesSetRow
            {
                Id = entity.Id,
                Name = entity.Name,
                Values = entity.Values
            };
        }
    }
}
