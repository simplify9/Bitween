using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.SubscriptionCategories;

public class Create(BitweenDbContext dbContext, RequestContext requestContext)
    : ICommandHandler<CreateSubscriptionCategoryModel,object>
{
    public async Task<object> Handle(CreateSubscriptionCategoryModel request)
    {
        var category = new SubscriptionCategory(request.Code, request.Description);
        dbContext.Add(category);
        await dbContext.SaveChangesAsync();
        return new
        {
            category.Id
        };
    }
}