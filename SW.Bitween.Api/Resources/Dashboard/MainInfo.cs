using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain;
using SW.Bitween.Domain.Accounts;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Dashboard;

[HandlerName("MainInfo")]
public class MainInfo(BitweenDbContext dbContext, RequestContext requestContext) : IQueryHandler<object>
{
    public async Task<object> Handle()
    {
        await requestContext.EnsurePermission(dbContext, Model.Permissions.Dashboard.View);

        var subscriptionsCount = await dbContext.Set<Subscription>().AsNoTracking().CountAsync();
        var documentCount = await dbContext.Set<Document>().AsNoTracking().CountAsync();
        var notifiersCount = await dbContext.Set<Notifier>().AsNoTracking().CountAsync();
        var usersCount = await dbContext.Set<Account>().AsNoTracking().CountAsync();
        var partnersCount = await dbContext.Set<Partner>().AsNoTracking().CountAsync();

        return new
        {
            subscriptionsCount,
            documentCount,
            notifiersCount,
            usersCount,
            partnersCount,
            LastUpdated = DateTime.UtcNow

        };
    }
}