using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain.DataSources;
using SW.Bitween.Model;
using SW.PrimitiveTypes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SW.Bitween.Resources.DataSourceStatements;

/// <summary>
/// Adds a statement to a data source.
///
/// Gated on <c>data-source-statements.create</c>, NOT on <c>data-sources.edit</c>. That separation
/// is the whole reason this is an entity: writing a query and changing a database password are
/// different jobs, and before this they needed the same right.
/// </summary>
public class Create(BitweenDbContext dbContext, RequestContext requestContext)
    : ICommandHandler<DataSourceStatementCreate, object>
{
    public async Task<object> Handle(DataSourceStatementCreate model)
    {
        var dataSourceId = model.DataSourceId;

        await requestContext.EnsurePermission(dbContext,
            Model.Permissions.DataSourceStatements.Create);

        var dataSource = await dbContext.Set<DataSource>().AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dataSourceId);

        if (dataSource == null)
            throw new SWNotFoundException($"DataSource with id '{dataSourceId}' was not found");

        // Only a relational source runs SQL. Refused rather than stored, because a statement on a
        // broker is configuration nothing will ever read — and silently keeping it is how people
        // conclude the feature is broken.
        if (dataSource.Kind != DataSourceKind.Relational)
            throw new SWException(
                $"'{dataSource.Name}' is a {dataSource.Kind} data source, and statements only mean "
                + "something to a Relational one.");

        await EnsureNameIsFree(dbContext, dataSourceId, model.Name, existingId: null);

        var entity = new DataSourceStatement
        {
            DataSourceId = dataSourceId,
            Name = model.Name.Trim(),
            Sql = model.Sql,
            CursorColumn = model.CursorColumn,
            KeyColumn = model.KeyColumn,
            Description = model.Description,
            WorkGroupId = model.WorkGroupId,
            Inactive = model.Inactive
        };

        dbContext.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity.Id;
    }

    /// <summary>
    /// Case-insensitively unique within the data source. The database index enforces uniqueness but
    /// its collation is provider-specific, so the case rule is applied here — where it can also say
    /// which statement it collided with, rather than surfacing as a constraint violation.
    /// </summary>
    internal static async Task EnsureNameIsFree(BitweenDbContext dbContext, int dataSourceId,
        string name, int? existingId)
    {
        var trimmed = (name ?? "").Trim();

        var clash = await dbContext.Set<DataSourceStatement>().AsNoTracking()
            .Where(s => s.DataSourceId == dataSourceId)
            .Where(s => existingId == null || s.Id != existingId)
            .ToListAsync();

        if (clash.Any(s => string.Equals(s.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            throw new SWException(
                $"This data source already has a statement called '{trimmed}'. Names are matched "
                + "without regard to case, because that is how the adapter resolves them.");
    }

    private class Validate : AbstractValidator<DataSourceStatementCreate>
    {
        public Validate()
        {
            RuleFor(i => i.DataSourceId).GreaterThan(0);
            RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
            RuleFor(i => i.Sql).NotEmpty();
            RuleFor(i => i.Description).MaximumLength(1000);
        }
    }
}
