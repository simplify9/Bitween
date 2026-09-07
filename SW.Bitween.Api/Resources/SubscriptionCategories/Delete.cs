using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.SubscriptionCategories;

[HandlerName("delete")]
public class Delete(BitweenDbContext dbContext, RequestContext requestContext)
    : ICommandHandler<int, DeleteSubscriptionCategoryModel,object>
{
    public async Task<object> Handle(int key, DeleteSubscriptionCategoryModel _)
    {
        var category = await dbContext.Set<SubscriptionCategory>().FindAsync(key);
        if (category is null)
            throw new SWValidationException("CATEGORY_NOT_FOUND", $"Category with id {key} was not found");

        if (await dbContext.Set<Subscription>().AnyAsync(i => i.CategoryId.Value == category.Id))
            throw new SWValidationException("CANT_BE_DELETED", "Categories with Subscriptions cant be deleted");

        dbContext.Remove(category);
        await dbContext.SaveChangesAsync();
        return null;
    }
}