﻿using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SW.EfCoreExtensions;
using SW.Infolink.Domain;
using SW.Infolink.Model;
using SW.PrimitiveTypes;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW.Infolink.Api.Resources.Subscriptions
{
    class Update : ICommandHandler<int, SubscriptionUpdate>
    {
        private readonly InfolinkDbContext dbContext;

        public Update(InfolinkDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        async public Task<object> Handle(int key, SubscriptionUpdate model)
        {
            var entity = await dbContext.FindAsync<Subscription>(key);

            dbContext.Entry(entity).SetProperties(model);

            entity.Schedules.Update(model.Schedules.Select(dto => new Schedule(dto.Recurrence, TimeSpan.Parse($"{dto.Days}.{dto.Hours}:{dto.Minutes}:0"), dto.Backwards)).ToList());
            entity.ReceiveSchedules.Update(model.ReceiveSchedules.Select(dto => new Schedule(dto.Recurrence, TimeSpan.Parse($"{dto.Days}.{dto.Hours}:{dto.Minutes}:0"), dto.Backwards)).ToList());

            entity.SetDictionaries(
                model.HandlerProperties.ToDictionary(),
                model.MapperProperties.ToDictionary(),
                model.ReceiverProperties.ToDictionary(),
                model.DocumentFilter.ToDictionary());

            await dbContext.SaveChangesAsync();
            return null;
        }

        private class Validate : AbstractValidator<SubscriptionUpdate>
        {
            public Validate(IServiceProvider serviceProvider)
            {
                RuleFor(i => i.Name).NotEmpty();

                When(i => i.Type == SubscriptionType.Receiving, () =>
                {
                    RuleFor(i => i.ReceiverId).NotEmpty();
                    RuleFor(i => i.ReceiveSchedules).NotEmpty();

                    When(i => i.ReceiverId != null, () =>
                    {
                        RuleFor(i => i.ReceiverProperties).CustomAsync(async (i, context, ct) =>
                        {
                            var serverless = serviceProvider.GetService<IServerlessService>();
                            await serverless.StartAsync(((SubscriptionUpdate)context.InstanceToValidate).ReceiverId);
                            var mustProps = (await serverless.GetExpectedStartupValues()).Where(p => p.Value.Optional == false).Select(p => p.Key);
                            var missing = mustProps.ToHashSet(StringComparer.OrdinalIgnoreCase).Except(i.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => p.Key));
                            if (missing.Any())
                                context.AddFailure($"Missing properties: {string.Join(",", missing) }");
                        });
                    });

                });
            }
        }
    }
}