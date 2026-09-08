using System.Globalization;

namespace SW.Bitween.NativeAdapters.Mapper;

/// <summary>
/// Applies rules to a source document tree and produces an output document tree.
/// </summary>
/// <remarks>
/// <para>
/// Knows nothing about JSON, XML or CSV: a reader turned the input into a <see cref="ValueNode"/>
/// and a writer turns the result back into text. A new format therefore costs a reader and a
/// writer, not a change here.
/// </para>
/// <para>
/// A pure function, which is the point — it is tested by asserting on the tree it returns, with no
/// format and no document text involved. That is only possible because it builds its own neutral
/// tree rather than writing into a format's builder as it goes.
/// </para>
/// </remarks>
public static class DocumentMapper
{
    /// <summary>
    /// Maps <paramref name="source"/> according to <paramref name="rules"/>.
    /// </summary>
    /// <exception cref="MappingFailedException">
    /// When any rule could not be applied. Every rule is attempted first, so the exception names
    /// all of them rather than stopping at the first.
    /// </exception>
    public static ValueNode Map(MappingRules rules, ValueNode? source, MappingContext context)
    {
        var errors = new List<MappingError>();

        ValueNode output;
        if (rules.Root is not null)
        {
            output = BuildList(rules.Root, source, context, errors, path: "");
        }
        else
        {
            var obj = ValueNode.Object();
            MapInto(obj, rules.Fields, rules.Loops, source, context, errors, path: "");
            output = obj;
        }

        if (errors.Count > 0) throw new MappingFailedException(errors);
        return output;
    }

    /// <summary>
    /// Applies one level of fields and loops. <paramref name="scope"/> is the node paths are read
    /// against — the whole document at the top level, the current item inside a loop.
    /// </summary>
    private static void MapInto(
        ObjectNode output,
        List<FieldRule> fields,
        List<LoopRule> loops,
        ValueNode? scope,
        MappingContext context,
        List<MappingError> errors,
        string path)
    {
        foreach (var field in fields)
        {
            var target = Describe(path, field.Target);

            if (field.Target.Count == 0)
            {
                errors.Add(new MappingError(target, "rule has no target"));
                continue;
            }

            if (!TryResolveField(field, scope, context, out var value, out var reason))
            {
                errors.Add(new MappingError(target, reason!));
                continue;
            }

            Values.PlaceAt(output, field.Target, ValueNode.Value(value));
        }

        foreach (var loop in loops)
        {
            var target = Describe(path, loop.Target);

            if (loop.Target.Count == 0)
            {
                errors.Add(new MappingError(target, "loop has no target"));
                continue;
            }

            Values.PlaceAt(output, loop.Target, BuildList(loop, scope, context, errors, path));
        }
    }

    /// <summary>
    /// Walks a loop's source list and builds the list it produces — a row per item, or a single
    /// value per item when the loop has an <see cref="LoopRule.Item"/> rule.
    /// </summary>
    private static ListNode BuildList(
        LoopRule loop,
        ValueNode? scope,
        MappingContext context,
        List<MappingError> errors,
        string path)
    {
        var target = Describe(path, loop.Target);
        var list = ValueNode.List();
        var over = Values.Resolve(scope, loop.Over);

        // A path that is absent, or holds something that is not a list, produces an empty list
        // rather than an error. An order with no lines is ordinary; so is an optional section.
        if (over is not ListNode items) return list;

        foreach (var item in items.Items)
        {
            if (loop.Where is not null && !Matches(loop.Where, item, out var whereError))
            {
                if (whereError is null) continue;

                // The condition itself is unusable, so every remaining item would report the same
                // thing. Say it once.
                errors.Add(new MappingError(target, whereError));
                break;
            }

            if (loop.Item is not null)
            {
                if (TryResolveField(loop.Item, item, context, out var value, out var reason))
                    list.Add(ValueNode.Value(value));
                else
                    errors.Add(new MappingError(target, reason!));
                continue;
            }

            var row = ValueNode.Object();
            MapInto(row, loop.Fields, loop.Loops, item, context, errors, target);
            list.Add(row);
        }

        return list;
    }

    private static bool TryResolveField(
        FieldRule field,
        ValueNode? scope,
        MappingContext context,
        out object? value,
        out string? reason)
    {
        reason = null;

        value = field.From.Kind switch
        {
            ValueSourceKind.Fixed => field.From.Value,
            ValueSourceKind.Path => Values.ResolveScalar(scope, field.From.Path),
            ValueSourceKind.Partner => context.PartnerValue(field.From.Key),
            ValueSourceKind.Global => context.GlobalValue(field.From.SetId, field.From.Key),
            _ => null,
        };

        if (field.Transform is not null &&
            !Transforms.TryApply(field.Transform, value, out value, out reason))
            return false;

        if (field.Lookup is not null)
            value = ApplyLookup(field.Lookup, value);

        // Into a separate variable: a failed conversion clears its output, so reading the message
        // off `value` would report "cannot convert 'null'" and hide what the value actually was.
        if (!Values.TryCoerce(value, field.Type, out var coerced))
        {
            reason = $"cannot convert '{Display(value)}' to {field.Type.ToString()!.ToLowerInvariant()}";
            return false;
        }

        value = coerced;
        return true;
    }

    /// <summary>
    /// Substitutes a value using the rule's table.
    /// </summary>
    /// <remarks>
    /// Keys are compared as text, because a lookup table is written by hand in the editor and the
    /// document it matches against may be XML or CSV, where <c>1</c> and <c>"1"</c> are the same
    /// thing. A miss gives the fallback, which is null unless the rule says otherwise.
    /// </remarks>
    private static object? ApplyLookup(LookupRule lookup, object? value)
    {
        if (value is null) return lookup.Fallback;

        var key = Values.TryCoerce(value, ValueType.String, out var text) ? text as string : null;
        return key is not null && lookup.Table.TryGetValue(key, out var mapped)
            ? mapped
            : lookup.Fallback;
    }

    /// <summary>
    /// Whether an item satisfies a loop's condition.
    /// </summary>
    /// <remarks>
    /// Returns false with a null <paramref name="error"/> for "does not match", and false with an
    /// error for "cannot be compared" — an ordering operator against a value that is not a number.
    /// A missing field does not match and is not an error, so filtering on an optional field skips
    /// the items that lack it instead of failing the exchange.
    /// </remarks>
    private static bool Matches(FilterRule filter, ValueNode item, out string? error)
    {
        error = null;
        var actual = Values.ResolveScalar(item, filter.Field);

        if (filter.Operator is FilterOperator.Equal or FilterOperator.NotEqual)
        {
            var same = string.Equals(AsComparableText(actual), AsComparableText(filter.Value),
                StringComparison.Ordinal);
            return filter.Operator == FilterOperator.Equal ? same : !same;
        }

        if (!TryAsNumber(actual, out var left) || !TryAsNumber(filter.Value, out var right))
        {
            // Only report when the rule itself is unusable. An item missing the field is skipped.
            if (actual is not null)
                error = $"cannot compare '{Display(actual)}' with '{Display(filter.Value)}' using {filter.Operator}";
            return false;
        }

        return filter.Operator switch
        {
            FilterOperator.GreaterThan => left > right,
            FilterOperator.GreaterThanOrEqual => left >= right,
            FilterOperator.LessThan => left < right,
            FilterOperator.LessThanOrEqual => left <= right,
            _ => false,
        };
    }

    private static bool TryAsNumber(object? value, out decimal number)
    {
        if (Values.TryCoerce(value, ValueType.Number, out var coerced) && coerced is decimal d)
        {
            number = d;
            return true;
        }
        number = 0;
        return false;
    }

    private static string? AsComparableText(object? value) =>
        value is null ? null : Values.TryCoerce(value, ValueType.String, out var text) ? text as string : null;

    private static string Display(object? value) => value switch
    {
        null => "null",
        decimal m => Values.FormatNumber(m),
        bool b => b ? "true" : "false",
        _ => value.ToString() ?? "",
    };

    /// <summary>The target as the user wrote it, for error messages.</summary>
    private static string Describe(string path, IReadOnlyList<string> target)
    {
        var joined = target.Count == 0 ? "(no target)" : string.Join(".", target);
        return path.Length == 0 ? joined : $"{path}[].{joined}";
    }
}
