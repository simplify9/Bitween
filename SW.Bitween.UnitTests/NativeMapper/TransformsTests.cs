using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SW.Bitween.NativeAdapters.Mapper;

namespace SW.Bitween.UnitTests.NativeMapper;

/// <summary>
/// The named functions that replace free-text expressions. One test per function, which is only
/// possible because each is an ordinary method rather than generated template code.
/// </summary>
[TestClass]
public class TransformsTests
{
    private static TransformRule Rule(string fn, params (string Name, object? Value)[] args)
    {
        var rule = new TransformRule { Fn = fn };
        foreach (var (name, value) in args)
            rule.Args[name] = value is null ? JValue.CreateNull() : JToken.FromObject(value);
        return rule;
    }

    private static object? Apply(TransformRule rule, object? value)
    {
        Assert.IsTrue(Transforms.TryApply(rule, value, out var result, out var error),
            $"expected '{rule.Fn}' to succeed, got: {error}");
        Assert.IsNull(error);
        return result;
    }

    private static string AssertFails(TransformRule rule, object? value)
    {
        Assert.IsFalse(Transforms.TryApply(rule, value, out _, out var error),
            $"expected '{rule.Fn}' to fail for '{value}'");
        Assert.IsNotNull(error, "a failure must explain itself");
        return error!;
    }

    /// <summary>
    /// Every name the editor can offer must actually resolve, and every implemented function must be
    /// listed — a name in one and not the other is a dropdown entry that fails at runtime, or a
    /// working function nobody can find.
    /// </summary>
    [TestMethod]
    public void EveryListedNameIsImplemented()
    {
        foreach (var name in Transforms.Names)
        {
            var error = Transforms.TryApply(Rule(name), "x", out _, out var reason) ? null : reason;
            Assert.IsFalse(error?.StartsWith("unknown transform") == true,
                $"'{name}' is listed but not implemented");
        }
    }

    [TestMethod]
    public void UnknownFunction_Fails() =>
        StringAssert.Contains(AssertFails(Rule("frobnicate"), "x"), "unknown transform");

    [TestMethod]
    public void MissingFunctionName_Fails() =>
        StringAssert.Contains(AssertFails(Rule(""), "x"), "no function name");

    /// <summary>
    /// Transforming an absent value leaves it absent. Turning null into <c>""</c> or <c>0</c> would
    /// invent data the source document never carried.
    /// </summary>
    [TestMethod]
    public void NullPassesThrough_ForEveryFunctionExceptDefaultIfEmpty()
    {
        foreach (var name in Transforms.Names.Where(n => n != "defaultIfEmpty"))
            Assert.IsNull(Apply(Rule(name, ("by", 2), ("amount", 2), ("format", "yyyy")), null),
                $"'{name}' should leave null alone");
    }

    // ── string functions ────────────────────────────────────────────────────────

    [TestMethod]
    public void Upper() => Assert.AreEqual("ALI", Apply(Rule("upper"), "Ali"));

    [TestMethod]
    public void Lower() => Assert.AreEqual("ali", Apply(Rule("lower"), "ALI"));

    [TestMethod]
    public void Trim() => Assert.AreEqual("Ali", Apply(Rule("trim"), "  Ali\t"));

    /// <summary>A number reaching a string function uses the same spelling a string target would.</summary>
    [TestMethod]
    public void StringFunctions_OnANumber_UseTheRoundTripSpelling() =>
        Assert.AreEqual("42", Apply(Rule("upper"), 42m));

    [TestMethod]
    public void Substring_WithStartAndLength() =>
        Assert.AreEqual("bcd", Apply(Rule("substring", ("start", 1), ("length", 3)), "abcdef"));

    /// <summary>No length means "to the end", which is what leaving the box empty means.</summary>
    [TestMethod]
    public void Substring_WithoutLength_RunsToTheEnd() =>
        Assert.AreEqual("cdef", Apply(Rule("substring", ("start", 2)), "abcdef"));

    /// <summary>A length past the end clamps rather than throwing.</summary>
    [TestMethod]
    public void Substring_LengthPastTheEnd_Clamps() =>
        Assert.AreEqual("ef", Apply(Rule("substring", ("start", 4), ("length", 99)), "abcdef"));

    [TestMethod]
    public void Substring_StartPastTheEnd_Fails() =>
        StringAssert.Contains(AssertFails(Rule("substring", ("start", 99)), "abc"), "outside a value");

    [TestMethod]
    public void Substring_NegativeLength_Fails() =>
        StringAssert.Contains(AssertFails(Rule("substring", ("start", 0), ("length", -1)), "abc"), "negative");

    [TestMethod]
    public void Replace() =>
        Assert.AreEqual("a-b-c", Apply(Rule("replace", ("find", " "), ("with", "-")), "a b c"));

    /// <summary>Replacing with nothing is how a user deletes a character.</summary>
    [TestMethod]
    public void Replace_WithNothing_Deletes() =>
        Assert.AreEqual("abc", Apply(Rule("replace", ("find", "-"), ("with", "")), "a-b-c"));

    [TestMethod]
    public void Concat() =>
        Assert.AreEqual("A1-JO", Apply(Rule("concat", ("with", "-JO")), "A1"));

    // ── numeric functions ───────────────────────────────────────────────────────

    [TestMethod]
    public void Multiply() => Assert.AreEqual(116m, Apply(Rule("multiply", ("by", 1.16)), 100m));

    /// <summary>A number arriving as text — every value from XML or CSV does — still multiplies.</summary>
    [TestMethod]
    public void Multiply_OnANumericString() =>
        Assert.AreEqual(116m, Apply(Rule("multiply", ("by", 1.16)), "100"));

    [TestMethod]
    public void Add() => Assert.AreEqual(12.5m, Apply(Rule("add", ("amount", 2.5)), 10m));

    [TestMethod]
    public void Round_ToDecimals() =>
        Assert.AreEqual(3.14m, Apply(Rule("round", ("decimals", 2)), 3.14159m));

    [TestMethod]
    public void Round_WithoutDecimals_IsWhole() =>
        Assert.AreEqual(3m, Apply(Rule("round"), 3.4m));

    /// <summary>
    /// Away-from-zero, because money is what gets rounded here and banker's rounding on a price
    /// surprises everyone who checks the arithmetic by hand.
    /// </summary>
    [TestMethod]
    public void Round_HalfGoesAwayFromZero()
    {
        Assert.AreEqual(3m, Apply(Rule("round"), 2.5m));
        Assert.AreEqual(-3m, Apply(Rule("round"), -2.5m));
        Assert.AreEqual(1.24m, Apply(Rule("round", ("decimals", 2)), 1.235m));
    }

    [TestMethod]
    public void NumericFunctions_OnNonNumericText_Fail()
    {
        StringAssert.Contains(AssertFails(Rule("multiply", ("by", 2)), "abc"), "needs a number");
        StringAssert.Contains(AssertFails(Rule("add", ("amount", 2)), "abc"), "needs a number");
        StringAssert.Contains(AssertFails(Rule("round"), "abc"), "needs a number");
    }

    [TestMethod]
    public void NumericFunctions_MissingOperand_Fail()
    {
        StringAssert.Contains(AssertFails(Rule("multiply"), 10m), "needs 'by'");
        StringAssert.Contains(AssertFails(Rule("add"), 10m), "needs 'amount'");
    }

    [TestMethod]
    public void Round_ImpossibleDecimals_Fails() =>
        StringAssert.Contains(AssertFails(Rule("round", ("decimals", 99)), 1m), "between 0 and 15");

    /// <summary>An argument typed into a text box arrives as a string, and must still work.</summary>
    [TestMethod]
    public void NumericArguments_MayArriveAsText() =>
        Assert.AreEqual(200m, Apply(Rule("multiply", ("by", "2")), 100m));

    // ── dates ───────────────────────────────────────────────────────────────────

    [TestMethod]
    public void FormatDate() =>
        Assert.AreEqual("2026-09-08", Apply(Rule("formatDate", ("format", "yyyy-MM-dd")), "2026-09-08T11:20:33Z"));

    [TestMethod]
    public void FormatDate_FromADateOnlyValue() =>
        Assert.AreEqual("08/09/2026", Apply(Rule("formatDate", ("format", "dd/MM/yyyy")), "2026-09-08"));

    [TestMethod]
    public void FormatDate_MissingFormat_Fails() =>
        StringAssert.Contains(AssertFails(Rule("formatDate"), "2026-09-08"), "needs 'format'");

    [TestMethod]
    public void FormatDate_UnreadableDate_Fails() =>
        StringAssert.Contains(AssertFails(Rule("formatDate", ("format", "yyyy")), "not a date"), "as a date");

    // ── defaultIfEmpty ──────────────────────────────────────────────────────────

    /// <summary>The one function whose job is to replace an absent value.</summary>
    [TestMethod]
    public void DefaultIfEmpty_ReplacesNull() =>
        Assert.AreEqual("UNKNOWN", Apply(Rule("defaultIfEmpty", ("value", "UNKNOWN")), null));

    /// <summary>
    /// An empty string counts as empty. A field present but blank is the same absence as a field
    /// that is missing, as far as anyone reading the output is concerned.
    /// </summary>
    [TestMethod]
    public void DefaultIfEmpty_ReplacesEmptyText() =>
        Assert.AreEqual("UNKNOWN", Apply(Rule("defaultIfEmpty", ("value", "UNKNOWN")), ""));

    [TestMethod]
    public void DefaultIfEmpty_LeavesARealValueAlone()
    {
        Assert.AreEqual("Ali", Apply(Rule("defaultIfEmpty", ("value", "UNKNOWN")), "Ali"));
        Assert.AreEqual(0m, Apply(Rule("defaultIfEmpty", ("value", "UNKNOWN")), 0m));
        Assert.AreEqual(false, Apply(Rule("defaultIfEmpty", ("value", "UNKNOWN")), false));
    }
}
