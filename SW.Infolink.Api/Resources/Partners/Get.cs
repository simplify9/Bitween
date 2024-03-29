﻿using Microsoft.EntityFrameworkCore;
using SW.Infolink.Domain;
using SW.PrimitiveTypes;
using SW.EfCoreExtensions;
using System.Linq;
using System.Threading.Tasks;
using SW.Infolink.Model;

namespace SW.Infolink.Resources.Partners
{
    class Get : IGetHandler<int>
    {
        private readonly InfolinkDbContext dbContext;

        public Get(InfolinkDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        async public Task<object> Handle(int key, bool lookup = false)
        {
            return await dbContext.Set<Partner>().AsNoTracking().
                Search("Id", key).
                Select(partner => new PartnerUpdate
                {
                    Name = partner.Name,

                    ApiCredentials = partner.ApiCredentials.Select(cred => new KeyAndValue
                    {
                        Key = cred.Name,
                        Value = $"{cred.Key.Remove(5)}...(hidden)"
                    }).ToList(),

                    Subscriptions = partner.Subscriptions.Select(sub => new SubscriptionSearch
                    {
                        Id = sub.Id,
                        Name = sub.Name,
                        Type = sub.Type,
                        DocumentId = sub.DocumentId,

                    }).ToList()

                }).AsNoTracking().SingleOrDefaultAsync();
        }
    }
}
