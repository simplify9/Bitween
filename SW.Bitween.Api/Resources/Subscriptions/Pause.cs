using System.Data.Common;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Subscriptions
{
    [HandlerName("pause")]
    public class Pause : ICommandHandler<int, SubscriptionPause,object>
    {
        private readonly BitweenDbContext _dbContext;
        private readonly RequestContext _requestContext;
        private readonly IInfolinkCache _cache;


        public Pause(BitweenDbContext dbContext, RequestContext requestContext, IInfolinkCache cache)
        {
            _dbContext = dbContext;
            _requestContext = requestContext;
            _cache = cache;
        }

        public async Task<object> Handle(int key, SubscriptionPause request)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.Subscriptions.Operate);

            var entity = await _dbContext.FindAsync<Subscription>(key);
            if (entity!.PausedOn == null)
                entity.Pause();
            else
                entity.UnPause();

            await _dbContext.SaveChangesAsync();
            // The receiving path reads PausedOn off the cached copy, so without this a paused
            // integration keeps taking messages for the rest of the cache's ten minutes. Resuming
            // has the mirror problem: its handler re-reads the cache, finds the copy still paused
            // and returns early, leaving everything it held on hold.
            await _cache.BroadcastRevoke();
            return new
            {
                entity.Id
            };
        }
    }
}