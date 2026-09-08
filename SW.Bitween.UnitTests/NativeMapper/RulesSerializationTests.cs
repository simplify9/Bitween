using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SW.Bitween.NativeAdapters.Mapper;
using ValueType = SW.Bitween.NativeAdapters.Mapper.ValueType;

namespace SW.Bitween.UnitTests.NativeMapper;

/// <summary>
/// The wire format between the editor and the mapper.
/// </summary>
/// <remarks>
/// Pinned because both sides depend on it and neither can see the other's types. The editor writes
/// what a JavaScript developer would write — camelCase names, lowercase enum values — so these
/// tests exist to make sure that keeps working rather than being an accident of Newtonsoft's
/// defaults.
/// </remarks>
[TestClass]
public class RulesSerializationTests
{
    private static MappingRules Read(string json) =>
        JsonConvert.DeserializeObject<MappingRules>(json)!;

    [TestMethod]
    public void CamelCasePropertyNames_AreAccepted()
    {
        var rules = Read("""
            { "version": 1, "sourceFormat": "json", "targetFormat": "json",
              "fields": [ { "target": ["a"], "from": { "kind": "path", "path": "x" }, "type": "number" } ] }
            """);

        Assert.AreEqual(1, rules.Version);
        Assert.AreEqual("json", rules.SourceFormat);
        Assert.AreEqual("a", rules.Fields[0].Target[0]);
        Assert.AreEqual("x", rules.Fields[0].From.Path);
    }

    /// <summary>Lowercase enum values, which is what the editor writes.</summary>
    [TestMethod]
    public void LowercaseEnumValues_AreAccepted()
    {
        var rules = Read("""
            { "fields": [
                { "target": ["a"], "from": { "kind": "path" }, "type": "number" },
                { "target": ["b"], "from": { "kind": "fixed" }, "type": "string" },
                { "target": ["c"], "from": { "kind": "partner" }, "type": "boolean" },
                { "target": ["d"], "from": { "kind": "global" } } ] }
            """);

        Assert.AreEqual(ValueSourceKind.Path, rules.Fields[0].From.Kind);
        Assert.AreEqual(ValueType.Number, rules.Fields[0].Type);
        Assert.AreEqual(ValueSourceKind.Fixed, rules.Fields[1].From.Kind);
        Assert.AreEqual(ValueType.String, rules.Fields[1].Type);
        Assert.AreEqual(ValueSourceKind.Partner, rules.Fields[2].From.Kind);
        Assert.AreEqual(ValueType.Boolean, rules.Fields[2].Type);
        Assert.AreEqual(ValueSourceKind.Global, rules.Fields[3].From.Kind);
    }

    [TestMethod]
    public void FilterOperators_AsLowercaseNames()
    {
        var rules = Read("""
            { "loops": [ { "over": "x", "where": { "field": "q", "operator": "greaterThan", "value": 0 } } ] }
            """);

        Assert.AreEqual(FilterOperator.GreaterThan, rules.Loops[0].Where!.Operator);
    }

    [TestMethod]
    public void TransformArguments_LandInArgs()
    {
        var rules = Read("""
            { "fields": [ { "target": ["a"], "from": { "kind": "fixed" },
                            "transform": { "fn": "multiply", "by": 1.16 } } ] }
            """);

        Assert.AreEqual("multiply", rules.Fields[0].Transform!.Fn);
        Assert.AreEqual(1.16, rules.Fields[0].Transform!.Args["by"].ToObject<double>());
    }

    /// <summary>An absent type means "leave the value as it is", not a default of string.</summary>
    [TestMethod]
    public void AbsentTypeIsNull() =>
        Assert.IsNull(Read("""{ "fields": [ { "target": ["a"], "from": { "kind": "fixed" } } ] }""")
            .Fields[0].Type);

    [TestMethod]
    public void NestedLoopsAndItemRules()
    {
        var rules = Read("""
            { "loops": [ { "over": "orders", "target": ["o"],
                           "loops": [ { "over": "lines", "target": ["l"],
                                        "item": { "from": { "kind": "path", "path": "sku" } } } ] } ] }
            """);

        Assert.AreEqual("lines", rules.Loops[0].Loops[0].Over);
        Assert.AreEqual("sku", rules.Loops[0].Loops[0].Item!.From.Path);
    }

    [TestMethod]
    public void RootLoop_ForRootArrayOutput() =>
        Assert.AreEqual("lines", Read("""{ "root": { "over": "lines" } }""").Root!.Over);

    /// <summary>Empty rules are valid — a mapping being built has none yet.</summary>
    [TestMethod]
    public void EmptyObject_GivesDefaults()
    {
        var rules = Read("{}");

        Assert.AreEqual(MappingRules.CurrentVersion, rules.Version);
        Assert.AreEqual("json", rules.SourceFormat);
        Assert.IsFalse(rules.Fields.Any());
        Assert.IsNull(rules.Root);
    }

    /// <summary>
    /// A round trip through the serialiser keeps the meaning, so the editor can read back what it
    /// saved without a translation step.
    /// </summary>
    [TestMethod]
    public void RoundTripsThroughSerialisation()
    {
        const string json = """
            { "version": 1, "sourceFormat": "json", "targetFormat": "json",
              "fields": [ { "target": ["customer","name"], "from": { "kind": "path", "path": "o.c" },
                            "transform": { "fn": "upper" }, "type": "string" } ],
              "loops": [ { "over": "o.l", "as": "line", "target": ["lines"],
                           "where": { "field": "qty", "operator": "greaterThan", "value": 0 },
                           "fields": [ { "target": ["sku"], "from": { "kind": "path", "path": "sku" } } ] } ] }
            """;

        var once = JsonConvert.SerializeObject(Read(json));
        var twice = JsonConvert.SerializeObject(Read(once));

        Assert.AreEqual(once, twice);
        var rules = Read(once);
        Assert.AreEqual("upper", rules.Fields[0].Transform!.Fn);
        Assert.AreEqual(FilterOperator.GreaterThan, rules.Loops[0].Where!.Operator);
        CollectionAssert.AreEqual(new[] { "customer", "name" }, rules.Fields[0].Target);
    }
}
