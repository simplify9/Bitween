using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.DataSources;

public class Update : ICommandHandler<int, DataSourceUpdate, object>
{
    private readonly BitweenDbContext _dbContext;
    private readonly RequestContext _requestContext;

    public Update(BitweenDbContext dbContext, RequestContext requestContext)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    public async Task<object> Handle(int key, DataSourceUpdate model)
    {
        await _requestContext.EnsurePermission(_dbContext, Model.Permissions.DataSources.Edit);

        var entity = await _dbContext.Set<DataSource>().FirstOrDefaultAsync(d => d.Id == key);
        if (entity == null)
            throw new SWNotFoundException($"DataSource with Id {key} not found");

        var nameTaken = await _dbContext.Set<DataSource>()
            .AnyAsync(d => d.Name == model.Name && d.Id != key);
        if (nameTaken)
            throw new SWException($"A data source named '{model.Name}' already exists.");

        // Secrets came out of Get masked, so put them back from what is stored. A form that only
        // changed the prefetch must not overwrite the password with a row of dots.
        var properties = Secrets.Merge(entity.Properties, model.Properties);

        entity.Name = model.Name;
        entity.AdapterId = model.AdapterId;
        entity.Kind = Create.ParseKind(model.Kind);
        entity.Properties = properties;
        entity.SecretProperties = Secrets.Declare(properties, model.SecretProperties);
        entity.Inactive = model.Inactive;
        entity.DeduplicationWindowDays = model.DeduplicationWindowDays;

        await _dbContext.SaveChangesAsync();

        // No cache to revoke and no adapter to restart from here: the supervisor reconciles against
        // these rows on its own loop, notices the fingerprint changed, and restarts the adapter.
        return null;
    }

    private class Validate : AbstractValidator<DataSourceUpdate>
    {
        public Validate()
        {
            RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
            RuleFor(i => i.AdapterId).NotEmpty().MaximumLength(200);
            RuleFor(i => i.DeduplicationWindowDays).GreaterThanOrEqualTo(0);
        }
    }
}
