using System.Collections.Generic;
using System.Threading.Tasks;
using SW.Bitween.Model;
using SW.Bitween.Services.DataSources;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.DataSources;

/// <summary>
/// The data source providers this deployment can offer, and what each one accepts.
///
/// Serving this from the adapters rather than from the front end is the whole point: a provider
/// added, a setting renamed, or a new allowed value appears in the UI the moment the adapter is
/// published, and cannot silently disagree with what the adapter actually reads.
/// </summary>
[HandlerName("Providers")]
public class Providers : IQueryHandler<object>
{
    private readonly BitweenDbContext _dbContext;
    private readonly RequestContext _requestContext;
    private readonly DataSourceProviderCatalog _catalog;

    public Providers(BitweenDbContext dbContext, RequestContext requestContext,
        DataSourceProviderCatalog catalog)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
        _catalog = catalog;
    }

    public async Task<object> Handle()
    {
        // Viewing is enough: this describes what Bitween can connect to, not what it is connected
        // to. Nothing here comes from a data source, so there is no credential to leak.
        await _requestContext.EnsurePermission(_dbContext, Model.Permissions.DataSources.View);

        return await _catalog.ListAsync();
    }
}
