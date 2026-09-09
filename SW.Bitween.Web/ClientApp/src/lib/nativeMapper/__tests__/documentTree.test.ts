import { describe, expect, it } from "vitest";
import {
  describeSample,
  itemScopeOf,
  itemShapeAt,
  listPaths,
  parseSample,
  readablePaths,
} from "../documentTree";

const ORDER = JSON.stringify({
  order: {
    customer: "Ali",
    net: 100,
    paid: true,
    note: null,
    address: { city: "Amman" },
    line: [
      { sku: "A1", qty: 2 },
      { sku: "B7", qty: 5 },
    ],
  },
});

const parse = (text: string) => parseSample(text, "json");

describe("reading a sample document", () => {
  it("reads nested objects", () => {
    const { root, error } = parse(ORDER);

    expect(error).toBeNull();
    expect(root?.kind).toBe("object");
    expect(readablePaths(root)).toContain("order.address.city");
  });

  it("reads every value type", () => {
    expect(readablePaths(parse(ORDER).root)).toEqual(
      expect.arrayContaining(["order.customer", "order.net", "order.paid", "order.note"]),
    );
  });

  /**
   * The difference from the old editor. It made a list behave as its own first
   * element, so `order.line.sku` appeared as something you could map — and it
   * silently meant the first line only.
   */
  it("does not offer paths inside a list as readable", () => {
    const paths = readablePaths(parse(ORDER).root);

    expect(paths).not.toContain("order.line.sku");
    expect(paths.some((p) => p.includes("line"))).toBe(false);
  });

  it("offers lists as list sources", () => {
    expect(listPaths(parse(ORDER).root)).toEqual(["order.line"]);
  });

  it("names an item's fields relative to the item", () => {
    // What a rule inside the list is written against: `sku`, not `order.line.sku`.
    expect(itemShapeAt(parse(ORDER).root, "order.line").map((n) => n.path)).toEqual(["sku", "qty"]);
  });

  it("counts the entries a list had in the sample", () => {
    const { root } = parse(ORDER);
    const line = root!.children[0].children.find((c) => c.key === "line");

    expect(line?.count).toBe(2);
  });

  /** A root array is a list source with an empty path, which is how a list says "the document". */
  it("treats a root array as a list source at the empty path", () => {
    const { root } = parse('[ { "id": 1 } ]');

    expect(root?.kind).toBe("list");
    expect(listPaths(root)).toEqual([""]);
    expect(itemShapeAt(root, "").map((n) => n.path)).toEqual(["id"]);
  });

  it("copes with an empty list", () => {
    const { root } = parse('{ "lines": [] }');

    expect(listPaths(root)).toEqual(["lines"]);
    expect(itemShapeAt(root, "lines")).toEqual([]);
  });

  it("copes with a list of plain values", () => {
    const { root } = parse('{ "tags": ["a","b"] }');

    expect(listPaths(root)).toEqual(["tags"]);
    expect(itemShapeAt(root, "tags")).toEqual([]);
  });

  it("says nothing for an empty sample", () => {
    const { root, error } = parse("   ");

    expect(root).toBeNull();
    expect(error).toBeNull();
  });

  it("reports a sample that is not JSON", () => {
    expect(parse("<order/>").error).toMatch(/not valid JSON/);
  });

  /** Honest rather than showing a JSON tree for a format we cannot read yet. */
  it("shows no tree for a format it cannot read", () => {
    const { root, error } = parseSample("<order/>", "xml");

    expect(root).toBeNull();
    expect(error).toMatch(/xml/);
  });
});

describe("describing a sample value", () => {
  it("quotes text and leaves other types bare", () => {
    expect(describeSample("Ali")).toBe('"Ali"');
    expect(describeSample(42)).toBe("42");
    expect(describeSample(true)).toBe("true");
    expect(describeSample(null)).toBe("null");
  });

  it("shortens long text so it fits on a line", () => {
    expect(describeSample("x".repeat(50))).toMatch(/…"$/);
  });
});

// ─── The scope inside a list ──────────────────────────────────────────────────

describe("itemScopeOf", () => {
  const doc = parseSample(
    '{ "order": { "line": [{ "sku": "A1", "tags": [{ "code": "fragile" }] }] } }',
    "json",
  ).root;

  it("hands back one entry's shape, named relative to the entry", () => {
    const scope = itemScopeOf(doc, "order.line");

    expect(readablePaths(scope)).toEqual(["sku"]);
    expect(listPaths(scope)).toEqual(["tags"]);
  });

  /** Which is what lets a list inside a list resolve its own list. */
  it("narrows again from that entry", () => {
    const inner = itemScopeOf(itemScopeOf(doc, "order.line"), "tags");

    expect(readablePaths(inner)).toEqual(["code"]);
  });

  it("is empty for a path that holds no list", () => {
    expect(readablePaths(itemScopeOf(doc, "order"))).toEqual([]);
    expect(readablePaths(itemScopeOf(null, "anything"))).toEqual([]);
  });

  /** An empty path means the document itself, for a source that is a bare array. */
  it("reads the document itself when the path is empty", () => {
    const array = parseSample('[{ "code": "A" }]', "json").root;

    expect(readablePaths(itemScopeOf(array, ""))).toEqual(["code"]);
  });
});
