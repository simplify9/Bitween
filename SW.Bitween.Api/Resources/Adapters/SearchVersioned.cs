using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SW.Bitween.Domain;
using SW.PrimitiveTypes;
using SW.Bitween.Model;

namespace SW.Bitween.Resources.Adapters
{
    [HandlerName("Versioned")]
    public class SearchVersioned : IQueryHandler<AdapterSearchRequest, object>
    {
        private readonly AdapterListing _listing;
        private readonly BitweenDbContext _dbContext;
        private readonly RequestContext _requestContext;

        public SearchVersioned(AdapterListing listing, BitweenDbContext dbContext,
            RequestContext requestContext)
        {
            _listing = listing;
            _dbContext = dbContext;
            _requestContext = requestContext;
        }

        public async Task<object> Handle(AdapterSearchRequest request)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.Subscriptions.View);

            var adapters = await _listing.List(request.Prefix);

            // Versions as a list of objects with a Key, which is the shape this endpoint has
            // always answered with. Catalog is where the tidier shape lives.
            return adapters.Select(a => new
            {
                a.Key,
                Versions = a.VersionPaths.Select(v => (object)new { Key = v }).ToList()
            });
        }
    }
}
