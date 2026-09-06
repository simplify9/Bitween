using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.DataSources;

public class Create : ICommandHandler<DataSourceCreate, object>
{
    private readonly BitweenDbContext _dbContext;
    private readonly RequestContext _requestContext;

    public Create(BitweenDbContext dbContext, RequestContext requestContext)
    {
        _dbContext = dbContext;
        _requestContext = requestContext;
    }

    public async Task<object> Handle(DataSourceCreate model)
    {
        await _requestContext.EnsurePermission(_dbContext, Model.Permissions.DataSources.Create);

        var nameTaken = await _dbContext.Set<DataSource>()
            .AnyAsync(d => d.Name == model.Name);
        if (nameTaken)
            throw new SWException($"A data source named '{model.Name}' already exists.");

        // Nothing is stored behind a sentinel on a create, so any that arrives — from a data source
        // copied out of Get, say — is dropped rather than saved as the literal string.
        var properties = Secrets.Merge(null, model.Properties);

        var entity = new DataSource
        {
            Name = model.Name,
            AdapterId = model.AdapterId,
            Kind = ParseKind(model.Kind),
            Properties = properties,
            SecretProperties = Secrets.Declare(properties, model.SecretProperties),
            Inactive = model.Inactive,
            DeduplicationWindowDays = model.DeduplicationWindowDays
        };

        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync();
        return entity.Id;
    }

    internal static DataSourceKind ParseKind(string kind) =>
        Enum.TryParse<DataSourceKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : DataSourceKind.Broker;

    private class Validate : AbstractValidator<DataSourceCreate>
    {
        public Validate()
        {
            RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
            RuleFor(i => i.AdapterId).NotEmpty().MaximumLength(200);

            // Zero is meaningful — it turns deduplication off — so only a negative is rejected.
            RuleFor(i => i.DeduplicationWindowDays).GreaterThanOrEqualTo(0);
        }
    }
}
