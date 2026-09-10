using System.Linq;
using Microsoft.EntityFrameworkCore;
using SW.Bitween.Domain;
using SW.Bitween.Model;
using SW.PrimitiveTypes;

namespace SW.Bitween.Resources.Xchanges;

/// <summary>
/// The exchange filters that cannot be handed to Searchy as-is, because they do not correspond to
/// one column: an id that should also match relatives, a status assembled from two columns and the
/// absence of a result row, a promoted-property substring that has to be case-folded.
/// </summary>
/// <remarks>
/// Shared by the search and by bulk retry, which needs "every exchange this filter matches" to mean
/// exactly the set the person was looking at when they chose it. The two drifting apart would be
/// invisible until a bulk retry quietly acted on a different set of exchanges than the one on
/// screen.
/// </remarks>
internal static class XchangeFilters
{
    /// <summary>
    /// Applies the special filters and removes them from <paramref name="searchyRequest"/>, leaving
    /// the plain per-column ones for Searchy to handle.
    /// </summary>
    internal static IQueryable<XchangeRow> ApplySpecialFilters(this IQueryable<XchangeRow> query,
        SearchyRequest searchyRequest, BitweenDbContext dbContext)
    {
        var condition = searchyRequest.Conditions.FirstOrDefault();
        if (condition == null)
            return query;

        var idFilters = condition.Filters.Where(f => f.Field == "Id").ToList();
        foreach (var idFilter in idFilters)
        {
            var value = idFilter.Value.ToString();
            switch (idFilter.Rule)
            {
                case SearchyRule.EqualsTo:
                    query = query.Where(i =>
                        i.Id == value || i.RetryFor == value || i.AggregationXchangeId == value);
                    break;
                case SearchyRule.Contains:
                    {
                        var valueAsArray = idFilter.ValueStringArray;
                        query = query.Where(i =>
                            valueAsArray.Any(v => i.RetryFor == v) ||
                            valueAsArray.Any(v => i.AggregationXchangeId == v) ||
                            valueAsArray.Any(v => i.Id == v)
                        );
                        break;
                    }


                default:
                    throw new SWValidationException("NOT_SUPPORTED", "Search query not supported");
            }

            condition.Filters.Remove(idFilter);
        }

        var statusFilters = condition.Filters.Where(f => f.Field == "StatusFilter").ToList();
        foreach (var statusFilter in statusFilters)
        {
            switch (statusFilter.Value)
            {
                case "0":
                    // "Still running" means no result row exists yet. Asking for it as
                    // Status == null reads as `x0.success IS NULL` on the left join, and
                    // Postgres cannot estimate that: it guesses one row, plans every join
                    // above it for one row, and picks per-row sequential scans of the small
                    // side tables. Measured on 1M exchanges that was 22.8s for 25 rows.
                    // NOT EXISTS asks the same question as an anti-join, which it can
                    // estimate — 34ms. Equivalent because success is NOT NULL, so a result
                    // row can never itself carry a null status.
                    query = query.Where(i =>
                        !dbContext.Set<XchangeResult>().Any(r => r.Id == i.Id));
                    break;
                case "1":
                    query = query.Where(i => i.Status == true && i.ResponseBad != true);
                    break;

                case "2":
                    query = query.Where(i => i.Status == true && i.ResponseBad == true);
                    break;

                case "3":
                    query = query.Where(i => i.Status == false);
                    break;

                default:
                    // The filter is removed below whether or not it matched, so falling through
                    // here used to drop it silently and widen the selection to everything. A
                    // search returning too much is merely wrong; bulk retry runs over whatever
                    // this selects, so an unreadable status has to be refused rather than ignored.
                    throw new SWValidationException("NOT_SUPPORTED",
                        $"'{statusFilter.Value}' is not an exchange status.");
            }

            condition.Filters.Remove(statusFilter);
        }

        var propertiesFilters = condition.Filters
            .Where(f => f.Field == "PromotedPropertiesRaw").ToList();
        foreach (var propertyFilter in propertiesFilters)
        {
            var value = propertyFilter.Value.ToString()!.ToLower();

            // Both sides lower-cased at query time. Promoted values keep the case the
            // payload had (see FilterService), so the column has to be folded here for
            // the search to stay case-insensitive. No index is lost: a Contains is a
            // leading-wildcard LIKE, which the b-tree on this column could never serve.
            query = query.Where(i => i.PromotedPropertiesRaw.ToLower().Contains(value));
            condition.Filters.Remove(propertyFilter);
        }

        return query;
    }
}
