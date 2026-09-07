using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SW.EfCoreExtensions;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.Subscriptions
{
    [HandlerName("savemapper")]
    public class SaveMapper(BitweenDbContext dbContext, IInfolinkCache BitweenCache,
        RequestContext requestContext) : ICommandHandler<int, SubscriptionSaveMapper, object>
    {
        public async Task<object> Handle(int key, SubscriptionSaveMapper model)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.Subscriptions.Edit);
            var entity = await dbContext.FindAsync<Subscription>(key);

            entity.MapperId = model.MapperId;
            entity.SetDictionaries(
                entity.HandlerProperties,
                model.MapperProperties.ToDictionary(),
                entity.ReceiverProperties,
                entity.DocumentFilter,
                entity.ValidatorProperties
            );

            await dbContext.SaveChangesAsync();
            await BitweenCache.BroadcastRevoke();
            return null;
        }

        private class Validate : AbstractValidator<SubscriptionSaveMapper>
        {
            public Validate(NativeAdapterDiscoveryService nativeAdapterDiscovery, IServiceProvider serviceProvider)
            {
                RuleFor(i => i.MapperId).NotEmpty();

                When(i => i.MapperId != null, () =>
                {
                    RuleFor(i => i.MapperProperties).CustomAsync(async (i, context, _) =>
                    {
                        var mapperId = ((SubscriptionSaveMapper)context.InstanceToValidate).MapperId;
                        var mustProps = Enumerable.Empty<string>();

                        if (mapperId.StartsWith(NativeAdapterDiscoveryService.NativePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var properties = nativeAdapterDiscovery.GetStartupValues(mapperId);
                            mustProps = properties.Where(p => !p.Value.Optional).Select(p => p.Key);
                        }
                        else
                        {
                            var serverless = serviceProvider.GetRequiredService<IServerlessService>();
                            await serverless.StartAsync(mapperId, null);
                            mustProps = (await serverless.GetExpectedStartupValues())
                                .Where(p => p.Value.Optional == false).Select(p => p.Key);
                        }

                        var missing = mustProps.ToHashSet(StringComparer.OrdinalIgnoreCase)
                            .Except(i.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => p.Key));
                        if (missing.Any())
                            context.AddFailure($"Missing: {string.Join(",", missing)}");
                    });
                });
            }
        }
    }
}
