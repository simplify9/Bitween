using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.Subscriptions
{
    [HandlerName("receivenow")]
    public class ReceiveNow(BitweenDbContext dbContext, RequestContext requestContext,
        SubscriptionSchedulerService subScheduler) : ICommandHandler<int, SubscriptionReceiveNow,object>
    {
        private readonly BitweenDbContext _dbContext = dbContext;
        private readonly RequestContext _requestContext = requestContext;
        private readonly SubscriptionSchedulerService _subScheduler = subScheduler;

        async public Task<object> Handle(int key, SubscriptionReceiveNow request)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.Subscriptions.Operate);

            var entity = await _dbContext.FindAsync<Subscription>(key);
            entity.SetReceiveNow();
            await _dbContext.SaveChangesAsync();

            await _subScheduler.RunNow(entity);
            return null;
        }
    }
}