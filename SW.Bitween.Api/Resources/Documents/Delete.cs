using SW.EfCoreExtensions;
using SW.Bitween.Domain;
using SW.PrimitiveTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.Gateway;

namespace SW.Bitween.Resources.Documents
{
public class Delete(BitweenDbContext dbContext, RequestContext requestContext, IInfolinkCache cache)
        : IDeleteHandler<int,object>
    {
        async public Task<object> Handle(int key)
        {
            await requestContext.EnsurePermission(dbContext, Model.Permissions.Documents.Delete);

            // Configuration pointing at this type is a real block — a subscription or a bus
            // gateway left behind would name a type that no longer exists. Said plainly, because
            // the database says it as a foreign-key violation, which reached the screen as a bare
            // 500 naming a constraint.
            if (await dbContext.Set<Subscription>().AnyAsync(s => s.DocumentId == key))
                throw new SWException(
                    "Cannot delete an information type that subscriptions still carry. Delete them, or point them at another type, first.");

            if (await dbContext.Set<BusGateway>().AnyAsync(g => g.DocumentId == key))
                throw new SWException(
                    "Cannot delete an information type that a bus gateway still listens for. Delete the gateway, or point it at another type, first.");

            // Exchanges are the deliberate exception. They are this type's history rather than
            // configuration depending on it, and history should not be able to strand a type
            // nobody uses any more — so they go with it.
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var xchangeIds = dbContext.Set<Xchange>().Where(x => x.DocumentId == key).Select(x => x.Id);

            // Scheduled retries are keyed by exchange id but have no relationship configured, so
            // nothing clears them on their own — they would be left pointing at exchanges that no
            // longer exist. Results, aggregations and promoted properties all cascade from the
            // exchange itself and need no help here.
            await dbContext.Set<DelayedRetry>().Where(d => xchangeIds.Contains(d.Id)).ExecuteDeleteAsync();
            await dbContext.Set<Xchange>().Where(x => x.DocumentId == key).ExecuteDeleteAsync();

            await dbContext.DeleteByKeyAsync<Document>(key);

            await transaction.CommitAsync();
            await cache.BroadcastRevoke();
            return null;
        }
    }
}
