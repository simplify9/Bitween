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
        public async Task<object> Handle(NotifierCreate request)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.Notifiers.Create);

            var notifier = new Notifier(request.Name);

            dbContext.Add(notifier);
            await dbContext.SaveChangesAsync();
            await cache.BroadcastRevoke();
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