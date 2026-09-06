using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Domain.Gateway;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.DataSources;

public class Get : IGetHandler<int, object>
{
    private readonly BitweenDbContext _dbContext;
    private readonly RequestContext _requestContext;

    public Get(BitweenDbContext dbContext, RequestContext requestContext)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    public async Task<object> Handle(int key)
    {
        await _requestContext.EnsurePermission(_dbContext, Model.Permissions.DataSources.View);

        var dataSource = await _dbContext.Set<DataSource>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == key);

        if (dataSource == null)
            throw new SWNotFoundException($"DataSource with id '{key}' was not found");

        return new DataSourceRow
        {
            Id = dataSource.Id,
            Name = dataSource.Name,
            AdapterId = dataSource.AdapterId,
            Kind = dataSource.Kind.ToString(),
            Inactive = dataSource.Inactive,
            DeduplicationWindowDays = dataSource.DeduplicationWindowDays,

            // Masked, always. This is the only endpoint that returns connection settings, so it is
            // the only place a broker password could leave the process.
            Properties = Secrets.Mask(dataSource.Properties, dataSource.SecretProperties),
            SecretProperties = dataSource.SecretProperties,

            LastKnownState = dataSource.LastKnownState,
            LastHeartbeatOn = dataSource.LastHeartbeatOn,
            LastException = dataSource.LastException,
            ConsecutiveFailures = dataSource.ConsecutiveFailures,
            OwnedByNode = dataSource.OwnedByNode,

            GatewayCount = await _dbContext.Set<BusGateway>()
                .CountAsync(gateway => gateway.DataSourceId == key)
        };
    }
}
