using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Domain.Gateway;
using SW.EfCoreExtensions;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.DataSources;

public class Delete : IDeleteHandler<int, object>
{
    private readonly BitweenDbContext _dbContext;
    private readonly RequestContext _requestContext;

    public Delete(BitweenDbContext dbContext, RequestContext requestContext)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    public async Task<object> Handle(int key)
    {
        await _requestContext.EnsurePermission(_dbContext, Model.Permissions.DataSources.Delete);

        // The database refuses this anyway — the foreign key restricts — but a raw constraint
        // violation tells an operator nothing about which gateway is in the way.
        var gateways = await _dbContext.Set<BusGateway>()
            .Where(gateway => gateway.DataSourceId == key)
            .Select(gateway => gateway.Name)
            .Take(5)
            .ToListAsync();

        if (gateways.Count > 0)
            throw new SWException(
                "Cannot delete a data source that still feeds bus gateways: " +
                string.Join(", ", gateways) +
                ". Point them at the internal bus, or delete them, first.");

        // Dedupe keys cascade with the data source, which is what makes deleting and recreating a
        // data source a genuine reset rather than one that silently suppresses the first messages.
        await _dbContext.DeleteByKeyAsync<DataSource>(key);
        await _dbContext.SaveChangesAsync();
        return null;
    }
}
