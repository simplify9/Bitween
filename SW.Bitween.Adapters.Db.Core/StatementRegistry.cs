using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SW.Bitween.Adapters.Db;

/// <summary>
/// The SQL this data source is allowed to run, by name.
///
/// The whole point is that SQL is configuration and message content is data. A mapper is a Scriban
/// template evaluated over an inbound payload; if it can emit SQL text, then whoever can get a
/// message into Bitween can steer a statement against the customer's database. So the statement is
/// named here, at configure time, and the message supplies parameter VALUES only.
/// </summary>
public class StatementRegistry
{
    readonly Dictionary<string, string> statements =
        new(StringComparer.OrdinalIgnoreCase);

    public StatementRegistry(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        JObject parsed;
        try
        {
            parsed = JObject.Parse(json);
        }
        catch (JsonException ex)
        {
            // Named rather than swallowed: an adapter that silently starts with no statements
            // fails later, on a message, as "unknown statement" — which sends whoever is debugging
            // it looking in the wrong place entirely.
            throw new ArgumentException(
                $"The Statements setting is not valid JSON: {ex.Message}. It should be an object "
                + "of name to SQL, for example {\"getOrder\": \"select * from orders where id = :id\"}.");
        }

        foreach (var property in parsed.Properties())
        {
            if (property.Value.Type != JTokenType.String)
                throw new ArgumentException(
                    $"Statement '{property.Name}' has to be a string of SQL, not a "
                    + $"{property.Value.Type}.");

            statements[property.Name] = property.Value.Value<string>();
        }
    }

    public IReadOnlyCollection<string> Names => statements.Keys;

    public int Count => statements.Count;

    /// <summary>
    /// The SQL to run for this request. A name resolves against the configured set; raw SQL is
    /// refused outright unless the data source allows it.
    /// </summary>
    public string Resolve(string name, string sql, bool allowAdHoc)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (statements.TryGetValue(name, out var found)) return found;

            throw new InvalidOperationException(
                $"'{name}' is not a statement this data source defines. Configured: "
                + (statements.Count == 0
                    ? "none — set the Statements property on the data source."
                    : string.Join(", ", statements.Keys.OrderBy(k => k))));
        }

        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException(
                "Nothing to run: give either the Name of a configured statement, or Sql if this "
                + "data source allows ad-hoc SQL.");

        if (!allowAdHoc)
            throw new InvalidOperationException(
                "This data source does not allow ad-hoc SQL, so a statement has to be named. SQL "
                + "sent with a message is SQL an inbound message can steer; define it on the data "
                + "source and pass parameters instead. AllowAdHocSql exists for the cases where "
                + "that trade is made deliberately.");

        return sql;
    }
}
