using System.Threading.Tasks;
using FluentValidation;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Notifiers
{
public class Create(BitweenDbContext dbContext, RequestContext requestContext, IInfolinkCache cache)
        : ICommandHandler<NotifierCreate,object>
    {
        private readonly BitweenDbContext _dbContext = dbContext;
        private readonly RequestContext _requestContext = requestContext;
        private readonly IInfolinkCache _cache = cache;

        public async Task<object> Handle(NotifierCreate request)
        {
            await _requestContext.EnsurePermission(_dbContext, Model.Permissions.Notifiers.Create);

            var notifier = new Notifier(request.Name);

            _dbContext.Add(notifier);
            await _dbContext.SaveChangesAsync();
            await _cache.BroadcastRevoke();
            return notifier.Id;
        }

        private class Validate : AbstractValidator<NotifierCreate>
        {
            public Validate()
            {
                RuleFor(i => i.Name).NotEmpty();
            }
        }
    }
}