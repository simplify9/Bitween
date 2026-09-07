using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain;
using SW.Bitween.Domain.Accounts;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Subscriptions;

/// <summary>
/// Clears one subscription's spent retry budget so its groups start retrying again.
/// </summary>
/// <remarks>
/// The policy-scoped reset finds subscriptions by policy id, which leaves an inline
/// <c>CustomRetryPolicy</c> unreachable: its counters are written like any other and then nothing can
/// clear them, so once exhausted that subscription would never retry again. This resets by
/// subscription instead, which also picks up counters left behind by groups that no longer exist.
/// </remarks>
[HandlerName("resetretryusage")]
public class ResetRetryUsage(BitweenDbContext dbContext, RequestContext requestContext)
    : ICommandHandler<int, SubscriptionRetryResetUsage, object>
{
    public async Task<object> Handle(int key, SubscriptionRetryResetUsage request)
    {
        await requestContext.EnsurePermission(dbContext, Model.Permissions.Subscriptions.Operate);

        if (!await dbContext.Set<Subscription>().AnyAsync(s => s.Id == key))
            throw new SWNotFoundException(key.ToString());

        // Scoped by subscription rather than by policy, so it cannot reach anyone else's counters no
        // matter which kind of policy this subscription uses.
        var query = dbContext.Set<RetryGroupUsage>().Where(u => u.SubscriptionId == key);

        if (request.GroupId.HasValue)
            query = query.Where(u => u.GroupId == request.GroupId.Value);

        await query.ExecuteDeleteAsync();
        return null;
    }
}
