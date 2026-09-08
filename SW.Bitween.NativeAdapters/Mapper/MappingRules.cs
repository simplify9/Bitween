using Newtonsoft.Json;

namespace SW.Bitween.NativeAdapters.Mapper;

/// <summary>
/// What the user configured, written down. The source of truth for a mapping — nothing is derived
/// from a generated artefact, and nothing is inferred from a sample document.
/// </summary>
/// <remarks>
/// <para>
/// The previous mapper saved the Scriban template it generated and threw the rules away, so opening
/// the editor again meant reverse-engineering the template with regular expressions. Rules are
/// source; a template is output. Storing only the output and recovering the source from it is
/// lossy, and it fails silently.
/// </para>
/// <para>
/// <see cref="Version"/> exists from the first release on purpose. Two mapper config formats already
/// shipped without one, so telling them apart now means sniffing for keys.
/// </para>
/// </remarks>
public class MappingRules
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>Format id of the incoming document — see <c>IDocumentFormat.Id</c>.</summary>
    public string SourceFormat { get; set; } = "json";

    /// <summary>Format id of the document to produce.</summary>
    public string TargetFormat { get; set; } = "json";

    public List<FieldRule> Fields { get; set; } = new();

    public List<LoopRule> Loops { get; set; } = new();

    /// <summary>
    /// When set, the whole output document is this list rather than an object.
    /// </summary>
    /// <remarks>
    /// Some partners expect a bare array — <c>[ {...}, {...} ]</c> — rather than an object with a
    /// list inside it. <see cref="Fields"/> and <see cref="Loops"/> are ignored when this is set,
    /// because a document is one thing or the other.
    /// </remarks>
    public LoopRule? Root { get; set; }
}

/// <summary>One output field: where its value comes from, and what it should end up as.</summary>
public class FieldRule
{
    /// <summary>
    /// Where to put the value, as path segments.
    /// </summary>
    /// <remarks>
    /// Segments rather than a dotted string, because a dotted string has to be split and then a key
    /// that genuinely contains a dot becomes unreachable — the old mapper turned a target of
    /// <c>file.txt</c> into a nested <c>{ "file": { "txt": … } }</c> with no way to ask for the
    /// literal key.
    /// </remarks>
    public List<string> Target { get; set; } = new();

    public ValueSource From { get; set; } = new();

    /// <summary>Optional named function applied to the value — see <see cref="Transforms"/>.</summary>
    public TransformRule? Transform { get; set; }

    /// <summary>Optional value substitution applied after <see cref="Transform"/>.</summary>
    public LookupRule? Lookup { get; set; }

    /// <summary>
    /// What the value should be coerced to. Null leaves it as the source produced it.
    /// </summary>
    /// <remarks>
    /// On the rule, not inferred from a target sample document. The old mapper read types out of
    /// whatever JSON was pasted into the editor, which meant the sample changed the output — so the
    /// same rules produced different documents depending on what someone had pasted.
    /// </remarks>
    public ValueType? Type { get; set; }
}

/// <summary>
/// Where a field's value comes from. Exactly one shape, chosen by <see cref="Kind"/>.
/// </summary>
/// <remarks>
/// The old model had six optional properties and picked whichever happened to be set, in a fixed
/// precedence order. That made "two sources set at once" representable, which is what the editor's
/// mode-switching bugs were: switching a field from one source to another had to remember to clear
/// all five others.
/// </remarks>
public class ValueSource
{
    public ValueSourceKind Kind { get; set; } = ValueSourceKind.Fixed;

    /// <summary>Dot-separated path into the source document. <see cref="ValueSourceKind.Path"/>.</summary>
    public string? Path { get; set; }

    /// <summary>Literal value. <see cref="ValueSourceKind.Fixed"/>.</summary>
    public object? Value { get; set; }

    /// <summary>Partner adapter-property key. <see cref="ValueSourceKind.Partner"/>.</summary>
    public string? Key { get; set; }

    /// <summary>Global values-set id. <see cref="ValueSourceKind.Global"/>, with <see cref="Key"/>.</summary>
    public string? SetId { get; set; }
}

public enum ValueSourceKind
{
    /// <summary>A literal written into the rule.</summary>
    Fixed,

    /// <summary>A path into the source document.</summary>
    Path,

    /// <summary>A property of the exchange's partner.</summary>
    Partner,

    /// <summary>A key in one of the global values sets.</summary>
    Global,
}

public enum ValueType
{
    String,
    Number,
    Boolean,
}

/// <summary>
/// A named function and its arguments.
/// </summary>
/// <remarks>
/// Named rather than a free-text expression, so there is no expression language to parse, sandbox or
/// generate. Each function is an ordinary method with its own test, and the editor can offer a list
/// instead of a syntax. If arbitrary arithmetic across several fields is ever genuinely needed, it
/// arrives as an explicit additional <see cref="ValueSourceKind"/> rather than by making this field
/// free text again.
/// </remarks>
public class TransformRule
{
    [JsonProperty("fn")]
    public string Fn { get; set; } = "";

    /// <summary>Arguments by name, e.g. <c>by</c> for multiply or <c>format</c> for formatDate.</summary>
    [JsonExtensionData]
    public Dictionary<string, Newtonsoft.Json.Linq.JToken> Args { get; set; } = new();
}

/// <summary>Replaces a value using a lookup table, with a fallback for anything not in it.</summary>
public class LookupRule
{
    public Dictionary<string, object?> Table { get; set; } = new();

    /// <summary>Used when the value is not a key in <see cref="Table"/>. Null means output null.</summary>
    public object? Fallback { get; set; }
}

/// <summary>Produces a list by walking a list in the source document.</summary>
public class LoopRule
{
    /// <summary>Path to the list to walk.</summary>
    public string Over { get; set; } = "";

    /// <summary>Name the item is known by inside this loop's paths and condition.</summary>
    public string As { get; set; } = "item";

    /// <summary>Where the resulting list goes, as path segments.</summary>
    public List<string> Target { get; set; } = new();

    /// <summary>Optional condition; items that do not match are skipped.</summary>
    public FilterRule? Where { get; set; }

    /// <summary>
    /// When set, each item produces a single value instead of an object — a list of strings or
    /// numbers rather than a list of records.
    /// </summary>
    /// <remarks>
    /// <c>{ "skus": ["A1","B7"] }</c> rather than <c>{ "lines": [{"sku":"A1"}] }</c>. Its
    /// <see cref="FieldRule.Target"/> is unused, since the value has nowhere to be named.
    /// <see cref="Fields"/> and <see cref="Loops"/> are ignored when this is set.
    /// </remarks>
    public FieldRule? Item { get; set; }

    public List<FieldRule> Fields { get; set; } = new();

    /// <summary>
    /// Loops nested inside this one, each walking a list found on the current item.
    /// </summary>
    /// <remarks>
    /// Nesting is structural rather than a flat list joined by parent ids, which is how the old
    /// model did it — that needed three separate recursive helpers to rebuild the full source and
    /// target paths, and a rule whose parent had been deleted became unreachable rather than
    /// invalid.
    /// </remarks>
    public List<LoopRule> Loops { get; set; } = new();
}

/// <summary>A condition on one field of the current item.</summary>
public class FilterRule
{
    public string Field { get; set; } = "";

    public FilterOperator Operator { get; set; } = FilterOperator.Equal;

    public object? Value { get; set; }
}

public enum FilterOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
}
