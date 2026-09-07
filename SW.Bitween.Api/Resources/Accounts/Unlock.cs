using System.Threading.Tasks;
using SW.Bitween.Domain.Accounts;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Accounts;

[HandlerName("unlock")]
public class Unlock(BitweenDbContext dbContext, RequestContext requestContext)
    : ICommandHandler<int, UnlockAccountModel, object>
{
    private readonly BitweenDbContext _dbContext = dbContext;
    private readonly RequestContext _requestContext = requestContext;

    public async Task<object> Handle(int key, UnlockAccountModel request)
    {
        await _requestContext.EnsurePermission(_dbContext, Model.Permissions.Users.Edit);

        var account = await _dbContext.Set<Account>().FindAsync(key);
        if (account is null)
            throw new SWValidationException("ACCOUNT_NOT_FOUND", $"No account exists with the id {key}");

        account.Unlock();
        await _dbContext.SaveChangesAsync();

        return null;
    }
}
