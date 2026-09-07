using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.Accounts;
using SW.Bitween.Model;
using SW.EfCoreExtensions;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Roles;

public class Search(BitweenDbContext dbContext, RequestContext requestContext) : ISearchyHandler
{
    public async Task<object> Handle(SearchyRequest searchyRequest, bool lookup = false, string searchPhrase = null)
    {
        await requestContext.EnsurePermission(dbContext, Model.Permissions.Roles.View);

        var query = from role in dbContext.Set<Role>()
            select new RoleRow
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystem = role.IsSystem,
                Permissions = role.Permissions,
                CreatedOn = role.CreatedOn,
                MemberCount = dbContext.Set<AccountRoleLink>().Count(l => l.RoleId == role.Id)
            };

        query = query.AsNoTracking();

        if (lookup)
            return await query.Search(searchyRequest.Conditions)
                .ToDictionaryAsync(k => k.Id.ToString(), v => v.Name);

        var result = await query.Search(searchyRequest.Conditions, searchyRequest.Sorts,
            searchyRequest.PageSize, searchyRequest.PageIndex).ToListAsync();

        // Built-in roles derive their grants from the catalog, so fill them in after the query.
        foreach (var row in result.Where(r => r.IsSystem))
            row.Permissions = Role.SystemPermissions(row.Id);

        return new SearchyResponse<RoleRow>
        {
            TotalCount = await query.Search(searchyRequest.Conditions).CountAsync(),
            Result = result
        };
    }
}
