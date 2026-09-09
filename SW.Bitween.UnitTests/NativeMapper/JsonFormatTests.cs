using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SW.Bitween.NativeAdapters.Mapper;
using SW.Bitween.NativeAdapters.Mapper.Formats;

namespace SW.Bitween.UnitTests.NativeMapper;

/// <summary>Reading JSON into a tree, and writing a tree back out.</summary>
[TestClass]
public class JsonFormatTests
{
    private static readonly JsonFormat Format = new();

    [TestMethod]
    public void ContentType_IsDeclared() => Assert.AreEqual("application/json", Format.ContentType);

    // ── reading ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Read_Scalars()
    {
        var doc = Format.Read("""{ "s": "Ali", "n": 42, "f": 9.5, "b": true, "z": null }""");

        Assert.AreEqual("Ali", Values.ResolveScalar(doc, "s"));
        Assert.AreEqual(42m, Values.ResolveScalar(doc, "n"));
        Assert.AreEqual(9.5m, Values.ResolveScalar(doc, "f"));
        Assert.AreEqual(true, Values.ResolveScalar(doc, "b"));
        Assert.IsNull(Values.ResolveScalar(doc, "z"));
    }

    /// <summary>
    /// A whole number and a fraction must both arrive as decimal, or the same rule would behave
    /// differently for <c>1</c> and <c>1.5</c>.
    /// </summary>
    [TestMethod]
    public void Read_AllNumbersAreDecimal()
    {
        var doc = Format.Read("""{ "whole": 1, "fraction": 1.5, "big": 12345678901234567890 }""");

        Assert.IsInstanceOfType<decimal>(Values.ResolveScalar(doc, "whole"));
        Assert.IsInstanceOfType<decimal>(Values.ResolveScalar(doc, "fraction"));
        Assert.IsInstanceOfType<decimal>(Values.ResolveScalar(doc, "big"));
    }

    /// <summary>
    /// The exact case that made <c>decimal</c> the right choice: read a rate, multiply a net by it,
    /// and get the answer someone checking by hand would get.
    /// </summary>
    [TestMethod]
    public void Read_KeepsDecimalFractionsExact()
    {
        var doc = Format.Read("""{ "net": 100, "rate": 1.16 }""");

        var net = (decimal)Values.ResolveScalar(doc, "net")!;
        var rate = (decimal)Values.ResolveScalar(doc, "rate")!;

        Assert.AreEqual(116m, net * rate);
    }

    /// <summary>
    /// A date stays as the document wrote it. Letting Newtonsoft parse it to <c>DateTime</c> would
    /// reformat and re-zone it, so a transform would see something other than what arrived.
    /// </summary>
    [TestMethod]
    public void Read_LeavesDatesAsText() =>
        Assert.AreEqual("2026-09-08T11:20:33Z",
            Values.ResolveScalar(Format.Read("""{ "d": "2026-09-08T11:20:33Z" }"""), "d"));

    [TestMethod]
    public void Read_NestedObjectsAndLists()
    {
        var doc = Format.Read("""{ "a": { "b": [ { "c": 1 }, { "c": 2 } ] } }""");

        var list = (ListNode)Values.Resolve(doc, "a.b")!;
        Assert.AreEqual(2, list.Items.Count);
        Assert.AreEqual(2m, Values.ResolveScalar(list.Items[1], "c"));
    }

    [TestMethod]
    public void Read_RootArray()
    {
        var doc = Format.Read("""[ { "id": 1 }, { "id": 2 } ]""");

        Assert.IsInstanceOfType<ListNode>(doc);
        Assert.AreEqual(2, ((ListNode)doc).Items.Count);
    }

    [TestMethod]
    public void Read_KeepsKeyOrder()
    {
        var doc = (ObjectNode)Format.Read("""{ "z": 1, "a": 2, "m": 3 }""");

        CollectionAssert.AreEqual(new[] { "z", "a", "m" }, doc.Keys.ToArray());
    }

    [TestMethod]
    public void Read_EmptyDocument_Fails() =>
        StringAssert.Contains(
            Assert.ThrowsException<DocumentFormatException>(() => Format.Read("   ")).Message,
            "empty");

    /// <summary>
    /// XML reaching the JSON reader is a misconfigured mapping, and the message has to say which
    /// format was wrong rather than surfacing a parser's own wording.
    /// </summary>
    [TestMethod]
    public void Read_NonJson_Fails()
    {
        foreach (var text in new[] { "<order><id>5</id></order>", "id,qty\n5,2", "{ oops" })
            StringAssert.Contains(
                Assert.ThrowsException<DocumentFormatException>(() => Format.Read(text)).Message,
                "not valid JSON",
                $"'{text}' should be rejected as JSON");
    }

    /// <summary>Two documents concatenated is not one document, and must not read as the first.</summary>
    [TestMethod]
    public void Read_TrailingSecondValue_Fails() =>
        StringAssert.Contains(
            Assert.ThrowsException<DocumentFormatException>(() => Format.Read("""{"a":1} {"b":2}""")).Message,
            "Additional text");

    // ── writing ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// The point of building a tree: every quote, comma and escape comes from the serialiser, so a
    /// value containing a quote cannot break the document. The old mapper needed <c>| json</c> on
    /// every field for this, and forgetting it produced invalid output.
    /// </summary>
    [TestMethod]
    public void Write_EscapesWithoutHelp()
    {
        var root = ValueNode.Object();
        root.Set("name", ValueNode.Value("""Ali "Abu" Hassan"""));
        root.Set("note", ValueNode.Value("line1\nline2\ttabbed \\ backslash"));

        var parsed = JObject.Parse(Format.Write(root));

        Assert.AreEqual("""Ali "Abu" Hassan""", parsed["name"]?.ToString());
        Assert.AreEqual("line1\nline2\ttabbed \\ backslash", parsed["note"]?.ToString());
    }

    /// <summary>
    /// The other thing the tree removes: a value containing a comma followed by a brace. The old
    /// backend stripped trailing commas with a regular expression over the whole rendered document,
    /// which ate this one out of the middle of a string.
    /// </summary>
    [TestMethod]
    public void Write_KeepsCommasInsideValues()
    {
        var root = ValueNode.Object();
        root.Set("note", ValueNode.Value("total is 5, } done"));

        Assert.AreEqual("total is 5, } done", JObject.Parse(Format.Write(root))["note"]?.ToString());
    }

    [TestMethod]
    public void Write_TypesAreNotQuoted()
    {
        var root = ValueNode.Object();
        root.Set("n", ValueNode.Value(42m));
        root.Set("b", ValueNode.Value(true));
        root.Set("z", ValueNode.Value(null));

        var parsed = JObject.Parse(Format.Write(root));

        Assert.AreEqual(JTokenType.Integer, parsed["n"]?.Type);
        Assert.AreEqual(JTokenType.Boolean, parsed["b"]?.Type);
        Assert.AreEqual(JTokenType.Null, parsed["z"]?.Type);
    }

    /// <summary>An empty loop result is an empty array, not a missing key or null.</summary>
    [TestMethod]
    public void Write_EmptyList()
    {
        var root = ValueNode.Object();
        root.Set("lines", ValueNode.List());

        var parsed = JObject.Parse(Format.Write(root));

        Assert.AreEqual(JTokenType.Array, parsed["lines"]?.Type);
        Assert.AreEqual(0, ((JArray)parsed["lines"]!).Count);
    }

    [TestMethod]
    public void Write_ThenRead_RoundTrips()
    {
        const string original = """
            { "id": "A1", "qty": 2, "price": 9.99, "ok": true, "note": null,
              "lines": [ { "sku": "X" }, { "sku": "Y" } ] }
            """;

        var once = Format.Write(Format.Read(original));
        var twice = Format.Write(Format.Read(once));

        Assert.AreEqual(once, twice);
        Assert.AreEqual("A1", JObject.Parse(once)["id"]?.ToString());
        Assert.AreEqual(9.99m, JObject.Parse(once)["price"]?.Value<decimal>());
    }
}
