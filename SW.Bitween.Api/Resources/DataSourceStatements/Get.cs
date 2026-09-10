using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.DataSourceStatements;

public class Get(BitweenDbContext dbContext, RequestContext requestContext)
    : IGetHandler<int, object>
{
    public async Task<object> Handle(int key)
    {
        await requestContext.EnsurePermission(dbContext, Model.Permissions.DataSourceStatements.View);

        var statement = await dbContext.Set<DataSourceStatement>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == key);

        if (statement == null) return null;

        return new DataSourceStatementUpdate
        {
            Name = statement.Name,
            Sql = statement.Sql,
            CursorColumn = statement.CursorColumn,
            KeyColumn = statement.KeyColumn,
            Description = statement.Description,
            WorkGroupId = statement.WorkGroupId,
            Inactive = statement.Inactive
        };
    }
}
