import { test, expect } from "@playwright/test";
import { signInAsAdmin } from "./helpers";
import { MAPPER_PARTNER, MAPPER_VALUES_SET } from "./global-setup";
import {
  SAMPLE,
  addFixedRule,
  addList,
  addListField,
  addListValue,
  addPathRule,
  buildFromSample,
  createSubscription,
  expectPreview,
  openDetail,
  openWithSample,
  preview,
  saveAndReload,
  setSourcePath,
  suggestionsFor,
  writeMapperProperties,
} from "./mapperHelpers";

/**
 * Every shape a JSON-to-JSON mapping can take, built in the editor and run.
 *
 * The mapping engine itself is covered exhaustively by the C# unit tests — every
 * transform argument, every coercion, every filter operator. What those cannot see
 * is whether the editor can *express* each of those shapes, and whether what it
 * saves reads back as the same mapping. That is what these are for: one pass per
 * shape, through the real UI, against the real server.
 */

test.beforeEach(async ({ page }) => {
  await signInAsAdmin(page);
});

/** Chooses whose partner values the preview resolves against. */
async function previewAsTestPartner(page: import("@playwright/test").Page) {
  await page
    .getByRole("combobox", { name: "Preview as partner" })
    .selectOption({ label: `${MAPPER_PARTNER} · 2 properties` });
}

/** Adds a field at the top level, leaving it selected and unassigned. */
async function addNamedRule(page: import("@playwright/test").Page, name: string) {
  await page.getByRole("button", { name: "Add a field", exact: true }).click();
  await page.getByRole("textbox", { name: "Output field name" }).last().fill(name);
}

/** Sets a rule's output type, which is behind its detail chevron. */
async function setType(
  page: import("@playwright/test").Page,
  name: string,
  type: "string" | "number" | "boolean",
) {
  await openDetail(page, name);
  await page.getByRole("combobox", { name: "Value type" }).selectOption(type);
  await openDetail(page, name);
}

// ─── Where a value can come from ──────────────────────────────────────────────

test("every kind of value reaches the output, and comes back as itself", async ({ page }) => {
  await openWithSample(page, { order: { customer: "Ali" } });

  // A scheduled job is required to have no partner of its own, so without choosing
  // one here the partner and global rows could never be seen working.
  await previewAsTestPartner(page);

  await addPathRule(page, "customer", "order.customer");
  await addFixedRule(page, "channel", "WEB");

  // A literal on a number target arrives as a number, not as the text "42".
  await addFixedRule(page, "copies", "42");
  await setType(page, "copies", "number");

  // A yes/no target offers two choices rather than free text, so "yes" can't be typed.
  await addNamedRule(page, "urgent");
  await setType(page, "urgent", "boolean");
  await page.getByRole("radio", { name: "Fixed" }).last().click();
  await page.getByRole("combobox", { name: "Fixed value" }).last().selectOption("true");

  await addNamedRule(page, "warehouse");
  await page.getByRole("radio", { name: "Partner" }).last().click();
  await page.getByRole("combobox", { name: "Partner property key" }).last().fill("WarehouseCode");

  await addNamedRule(page, "network");
  await page.getByRole("radio", { name: "Global" }).last().click();
  await page
    .getByRole("combobox", { name: "Global values set" })
    .last()
    .selectOption(MAPPER_VALUES_SET.id);
  // The set's keys are a list to choose from, so a typo cannot reach the server.
  await page.getByRole("combobox", { name: "Global value key" }).last().selectOption("channel");

  await expectPreview(page, '"customer": "Ali"');
  await expect(preview(page)).toContainText('"channel": "WEB"');
  await expect(preview(page)).toContainText('"copies": 42');
  await expect(preview(page)).toContainText('"urgent": true');
  await expect(preview(page)).toContainText('"warehouse": "WH-7"');
  await expect(preview(page)).toContainText('"network": "EDI"');

  // ── Each source comes back as the kind it was, not as a path ────────────────
  await saveAndReload(page);

  await expect(page.getByRole("combobox", { name: "Partner property key" })).toHaveValue(
    "WarehouseCode",
  );
  await expect(page.getByRole("combobox", { name: "Global values set" })).toHaveValue(
    MAPPER_VALUES_SET.id,
  );
  await expect(page.getByRole("combobox", { name: "Global value key" })).toHaveValue("channel");

  // A saved mapping does not carry the partner it was previewed against, so the
  // reloaded editor has to be told again — and until it is, the partner rows are
  // empty rather than stale.
  await previewAsTestPartner(page);
  await expectPreview(page, '"warehouse": "WH-7"');
});

test("a partner key or a global key that is not there leaves the field empty", async ({ page }) => {
  await openWithSample(page, { order: { customer: "Ali" } });
  await previewAsTestPartner(page);

  await addNamedRule(page, "missingProp");
  await page.getByRole("radio", { name: "Partner" }).last().click();
  await page.getByRole("combobox", { name: "Partner property key" }).last().fill("NotAProperty");

  await addNamedRule(page, "missingGlobal");
  await page.getByRole("radio", { name: "Global" }).last().click();
  await page
    .getByRole("combobox", { name: "Global values set" })
    .last()
    .selectOption(MAPPER_VALUES_SET.id);
  await page.getByRole("combobox", { name: "Global value key" }).last().selectOption("region");

  await expectPreview(page, '"missingGlobal": "AMMAN"');
  // Empty rather than a failure: a partner not having a property is a normal state,
  // unlike a value that cannot be converted.
  await expect(preview(page)).toContainText('"missingProp": null');
  await expect(page.getByText(/could not be applied/)).toHaveCount(0);
});

test("a rule with no partner chosen resolves nothing, and says nothing is wrong", async ({
  page,
}) => {
  await openWithSample(page, { order: { customer: "Ali" } });

  await addNamedRule(page, "warehouse");
  await page.getByRole("radio", { name: "Partner" }).last().click();
  await page.getByRole("combobox", { name: "Partner property key" }).last().fill("WarehouseCode");

  // The default: no partner, so the value is absent — which is exactly what a
  // scheduled job's own mapping would produce, and why the picker exists.
  await expectPreview(page, '"warehouse": null');

  await previewAsTestPartner(page);
  await expectPreview(page, '"warehouse": "WH-7"');
});

// ─── What a value is written as ───────────────────────────────────────────────

test("values are written as the partner asked for, or the rule is named", async ({ page }) => {
  await openWithSample(page, { qty: "12", flag: "true", net: 100, name: "Ali" });

  await addPathRule(page, "qty", "qty");
  await setType(page, "qty", "number");

  await addPathRule(page, "flag", "flag");
  await setType(page, "flag", "boolean");

  await addPathRule(page, "net", "net");
  await setType(page, "net", "string");

  await addPathRule(page, "asItComes", "net");

  await expectPreview(page, '"qty": 12');
  await expect(preview(page)).toContainText('"flag": true');
  await expect(preview(page)).toContainText('"net": "100"');
  // No type at all leaves the number a number.
  await expect(preview(page)).toContainText('"asItComes": 100');

  // ── And a conversion that cannot work fails loudly ─────────────────────────
  await addPathRule(page, "broken", "name");
  await setType(page, "broken", "number");

  await expect(page.getByText(/cannot convert 'Ali' to number/).first()).toBeVisible({
    timeout: 15000,
  });
});

// ─── Transforms ───────────────────────────────────────────────────────────────

/** Every transform, its arguments, and what it makes of the sample below. */
const TRANSFORM_CASES: {
  field: string;
  path: string;
  fn: string;
  args?: [string, string][];
  expect: string;
}[] = [
  { field: "up", path: "text", fn: "upper", expect: '"up": "HELLO"' },
  { field: "down", path: "shout", fn: "lower", expect: '"down": "loud"' },
  { field: "tidy", path: "padded", fn: "trim", expect: '"tidy": "pad"' },
  {
    field: "part",
    path: "text",
    fn: "substring",
    args: [
      ["Take part of the text — Start at", "1"],
      ["Take part of the text — Length", "3"],
    ],
    expect: '"part": "ell"',
  },
  {
    field: "swapped",
    path: "text",
    fn: "replace",
    args: [
      ["Replace text — Find", "l"],
      ["Replace text — Replace with", "L"],
    ],
    expect: '"swapped": "heLLo"',
  },
  {
    field: "joined",
    path: "text",
    fn: "concat",
    args: [["Append text — Append", "!"]],
    expect: '"joined": "hello!"',
  },
  {
    field: "rounded",
    path: "n",
    fn: "round",
    args: [["Round — Decimals", "2"]],
    expect: '"rounded": 10.57',
  },
  {
    field: "times",
    path: "n",
    fn: "multiply",
    args: [["Multiply — By", "2"]],
    expect: '"times": 21.134',
  },
  {
    field: "plus",
    path: "n",
    fn: "add",
    args: [["Add — Amount", "1"]],
    expect: '"plus": 11.567',
  },
  {
    field: "when",
    path: "date",
    fn: "formatDate",
    args: [["Format a date — Format", "dd MMM yyyy"]],
    expect: '"when": "04 Mar 2026"',
  },
  {
    field: "filled",
    path: "blank",
    fn: "defaultIfEmpty",
    args: [["Use a default when empty — Default", "NONE"]],
    expect: '"filled": "NONE"',
  },
];

test("every transform has the argument boxes it needs, and produces its value", async ({
  page,
}) => {
  test.slow();

  await openWithSample(page, {
    text: "hello",
    shout: "LOUD",
    padded: "  pad  ",
    n: 10.567,
    date: "2026-03-04",
    blank: "",
  });

  for (const testCase of TRANSFORM_CASES) {
    await addPathRule(page, testCase.field, testCase.path);
    await openDetail(page, testCase.field);
    await page.getByRole("combobox", { name: "Transform" }).selectOption(testCase.fn);

    // The boxes are named from the function, so a function whose arguments the
    // editor spells differently to the server would show up right here.
    //
    // By label rather than by role: an argument box may be a plain input, or one
    // carrying a suggestion list — and an `<input list=…>` reports as a combobox, not
    // a textbox. The name is what identifies it either way.
    for (const [label, value] of testCase.args ?? []) {
      const box = page.getByLabel(label, { exact: true });
      // Some arguments are a closed list now, so the gesture depends on the control.
      if ((await box.evaluate((el) => el.tagName)) === "SELECT") await box.selectOption(value);
      else await box.fill(value);
    }

    await openDetail(page, testCase.field);
  }

  for (const testCase of TRANSFORM_CASES)
    await expect(preview(page)).toContainText(testCase.expect, { timeout: 20000 });
});

test("a transform leaves an absent value absent, unless it is there to replace one", async ({
  page,
}) => {
  await openWithSample(page, { there: "yes" });

  // `missing` names nothing in the document. Uppercasing nothing is nothing — not
  // "" — so the field is absent rather than becoming an empty string.
  await addPathRule(page, "shouted", "there");
  await openDetail(page, "shouted");
  await page.getByRole("combobox", { name: "Transform" }).selectOption("upper");
  await openDetail(page, "shouted");

  await addNamedRule(page, "quiet");
  await setSourcePath(page, "");
  await openDetail(page, "quiet");
  await page.getByRole("combobox", { name: "Transform" }).selectOption("upper");
  await openDetail(page, "quiet");

  await addNamedRule(page, "defaulted");
  await openDetail(page, "defaulted");
  await page.getByRole("combobox", { name: "Transform" }).selectOption("defaultIfEmpty");
  await page
    .getByRole("textbox", { name: "Use a default when empty — Default", exact: true })
    .fill("NONE");
  await openDetail(page, "defaulted");

  await expectPreview(page, '"shouted": "YES"');
  await expect(preview(page)).toContainText('"quiet": null');
  await expect(preview(page)).toContainText('"defaulted": "NONE"');
});

// ─── Lookup tables ────────────────────────────────────────────────────────────

test("a lookup runs after the transform, not before it", async ({ page }) => {
  await openWithSample(page, { country: "jo" });

  await addPathRule(page, "countryName", "country");
  await openDetail(page, "countryName");

  await page.getByRole("combobox", { name: "Transform" }).selectOption("upper");
  await page.getByRole("checkbox", { name: "Substitute values from a table" }).check();
  await page.getByRole("button", { name: "Add incoming value" }).click();
  // Keyed on the *transformed* value: "jo" uppercased is "JO", and it is "JO" the
  // table is asked about. A table keyed on "jo" would miss.
  await page.getByRole("textbox", { name: "Incoming value 1" }).fill("JO");
  await page.getByRole("textbox", { name: "Becomes 1" }).fill("Jordan");

  await expectPreview(page, '"countryName": "Jordan"');
});

// ─── Lists ────────────────────────────────────────────────────────────────────

const OPERATOR_CASES: { field: string; operator: string; expect: RegExp }[] = [
  { field: "eq", operator: "equal", expect: /"eq":\s*\[\s*2\s*\]/ },
  { field: "ne", operator: "notEqual", expect: /"ne":\s*\[\s*1,\s*3\s*\]/ },
  { field: "gt", operator: "greaterThan", expect: /"gt":\s*\[\s*3\s*\]/ },
  { field: "ge", operator: "greaterThanOrEqual", expect: /"ge":\s*\[\s*2,\s*3\s*\]/ },
  { field: "lt", operator: "lessThan", expect: /"lt":\s*\[\s*1\s*\]/ },
  { field: "le", operator: "lessThanOrEqual", expect: /"le":\s*\[\s*1,\s*2\s*\]/ },
];

test("every filter comparison keeps the entries it should", async ({ page }) => {
  test.slow();

  await openWithSample(page, { line: [{ qty: 1 }, { qty: 2 }, { qty: 3 }] });

  for (const { field, operator } of OPERATOR_CASES) {
    const list = await addList(page, field, "line");
    await page.getByRole("button", { name: `Settings for the list ${field}` }).click();

    await page.getByRole("checkbox", { name: "Only some entries" }).last().check();
    await page.getByRole("textbox", { name: "Filter field" }).last().fill("qty");
    await page.getByRole("combobox", { name: "Filter comparison" }).last().selectOption(operator);
    await page.getByRole("textbox", { name: "Filter value" }).last().fill("2");

    await page.getByRole("button", { name: `Settings for the list ${field}` }).click();

    // A list of plain values, so what survived the filter reads straight off the
    // preview rather than through a wrapper object.
    await addListValue(list, field, "qty");
  }

  for (const { expect: shape } of OPERATOR_CASES)
    await expect(preview(page)).toHaveText(shape, { timeout: 20000 });
});

test("a list can walk the incoming document itself", async ({ page }) => {
  // A partner that sends a bare array, which is not reachable by any path.
  await openWithSample(page, [
    { sku: "A1", qty: 2 },
    { sku: "B7", qty: 5 },
  ]);

  await page.getByRole("button", { name: "Add a list", exact: true }).click();
  await page.getByRole("textbox", { name: "Output list name" }).fill("lines");
  await page.getByRole("combobox", { name: "Source list" }).selectOption({ label: "(the document)" });

  const lines = page.getByRole("group", { name: "Rules for the list lines" });
  await addListField(lines, "lines", "code", "sku");

  await expectPreview(page, '"code": "A1"');
  await expect(preview(page)).toContainText('"code": "B7"');

  // "The document is the list" and "nothing is walked" are different states, and
  // the row says which by the word in front of the dropdown.
  await expect(page.getByText("for each of")).toBeVisible();
  await saveAndReload(page);
  await expect(page.getByRole("combobox", { name: "Source list" })).toHaveValue("p:");
});

test("an empty source list gives an empty list, and so does one that is not there", async ({
  page,
}) => {
  await openWithSample(page, { line: [], other: { deep: 1 } });

  await addList(page, "lines", "line");
  const lines = page.getByRole("group", { name: "Rules for the list lines" });
  await addListField(lines, "lines", "code", "sku");

  // The sample has no entry to take field names from, so there is nothing to
  // suggest — and the mapping is still expressible, because the path was typed.
  // While this box was a dropdown, a sample like this one could not be mapped at all.
  const field = lines.getByRole("combobox", { name: "Source field" });
  expect(await suggestionsFor(page, field)).toEqual([]);
  await expect(field).toHaveValue("sku");
  await expect(lines.getByText("⚠")).toBeVisible();

  // An empty array is not an error, and neither is a path naming nothing: a partner
  // sending no lines today is a normal document.
  await expectPreview(page, '"lines": []');
  await expect(page.getByText(/could not be applied/)).toHaveCount(0);

  // And it is a real mapping, not an accident of the sample: the same rules against
  // a document that does have lines produce them.
  await page
    .getByRole("textbox", { name: "Sample source document" })
    .fill(JSON.stringify({ line: [{ sku: "A1" }, { sku: "B7" }] }, null, 2));

  await expectPreview(page, '"code": "A1"');
  await expect(preview(page)).toContainText('"code": "B7"');
  await expect(lines.getByText("⚠")).toHaveCount(0);
});

test("the whole output can be a list, of records or of plain values", async ({ page }) => {
  await openWithSample(page);

  await page.getByRole("checkbox", { name: /The whole output is a list/ }).check();
  await page.getByRole("combobox", { name: "Source list" }).selectOption("p:order.line");

  const root = page.getByRole("group", { name: "Rules for the list at the root" });
  await addListField(root, "the root list", "code", "sku");

  // A bare array, with no object wrapped round it — what the old mapper needed a
  // separate output mode for.
  await expectPreview(page, '"code": "A1"');
  await expect(preview(page)).toHaveText(/^\[[\s\S]*\]$/);

  // ── And the same thing as plain values ─────────────────────────────────────
  // A list holds one or the other, and says so by what it will let you add: the
  // record's field has to go before the value can be put in its place.
  await root.getByRole("button", { name: "Remove the rule for code" }).click();
  await addListValue(root, "the root list", "sku");

  await expect(preview(page)).toHaveText(/^\[\s*"A1",\s*"B7"\s*\]$/, { timeout: 15000 });
});

test("an entry written into a list may read the document, the partner, and hold a list", async ({
  page,
}) => {
  await openWithSample(page, {
    order: { ref: "ORD-9", line: [{ sku: "A1", tag: [{ code: "cold" }] }] },
  });
  await previewAsTestPartner(page);

  await addList(page, "lines", "order.line");
  const lines = page.getByRole("group", { name: "Rules for the list lines" });
  await addListField(lines, "lines", "sku", "sku");

  // A header entry the partner expects. It is built from ordinary rules, so unlike
  // the old mapper's literal JSON it can read anything a walked entry can.
  await lines.getByRole("button", { name: "Add an entry to lines" }).click();
  const entry = page.getByRole("group", { name: "Rules for entry 1" });

  await entry.getByRole("button", { name: "Add a field to entry 1" }).click();
  await entry.getByRole("textbox", { name: "Output field name" }).last().fill("sku");
  await entry.getByRole("radio", { name: "Fixed" }).last().click();
  await entry.getByRole("textbox", { name: "Fixed value" }).last().fill("HEADER");

  await entry.getByRole("button", { name: "Add a field to entry 1" }).click();
  await entry.getByRole("textbox", { name: "Output field name" }).last().fill("ref");
  await setSourcePath(entry, "order.ref");

  await entry.getByRole("button", { name: "Add a field to entry 1" }).click();
  await entry.getByRole("textbox", { name: "Output field name" }).last().fill("warehouse");
  await entry.getByRole("radio", { name: "Partner" }).last().click();
  await entry.getByRole("combobox", { name: "Partner property key" }).last().fill("WarehouseCode");

  await expectPreview(page, '"sku": "HEADER"');
  await expect(preview(page)).toContainText('"ref": "ORD-9"');
  await expect(preview(page)).toContainText('"warehouse": "WH-7"');
  // Written first, then one per walked entry.
  await expect(preview(page)).toHaveText(/HEADER[\s\S]*"sku": "A1"/);

  // A written entry reads from where the list sits, not from an entry that was
  // never walked — so `order.ref` resolves rather than being relative to a line.
  await saveAndReload(page);
  await expect(page.getByRole("group", { name: "Rules for entry 1" })).toBeVisible();
});

test("a list nested three deep still reads the entry it sits in", async ({ page }) => {
  await openWithSample(page, {
    order: {
      line: [
        {
          sku: "A1",
          box: [{ id: "B1", item: [{ serial: "S1" }, { serial: "S2" }] }],
        },
      ],
    },
  });

  await buildFromSample(page, {
    line: [{ sku: "", box: [{ id: "", item: [{ serial: "" }] }] }],
  });

  await expectPreview(page, '"serial": "S1"');
  await expect(preview(page)).toContainText('"serial": "S2"');
  await expect(preview(page)).toContainText('"id": "B1"');
  await expect(preview(page)).toContainText('"sku": "A1"');
});

// ─── The shape of the output ──────────────────────────────────────────────────

test("dotted names build objects, and two rules share one", async ({ page }) => {
  await openWithSample(page, { c: "Amman", k: "JO", n: "Ali" });

  await addPathRule(page, "billing.city", "c");
  await addPathRule(page, "billing.country", "k");
  await addPathRule(page, "name", "n");

  await expectPreview(page, '"city": "Amman"');
  await expect(preview(page)).toHaveText(/"billing":\s*\{[\s\S]*"country": "JO"[\s\S]*\}/);

  // One branch in the tree rather than two rows that happen to share a prefix, and
  // the rows inside it show only their last segment.
  const branch = page.getByRole("group", { name: "Fields inside billing" });
  await expect(branch.getByRole("textbox", { name: "Output field name" })).toHaveCount(2);
  await expect(branch.getByRole("textbox", { name: "Output field name" }).first()).toHaveValue(
    "city",
  );
});

test("the rules are written in the order they are listed", async ({ page }) => {
  await openWithSample(page, { a: 1, b: 2 });

  await addPathRule(page, "second", "b");
  await addPathRule(page, "first", "a");

  // The order the rules sit in, not alphabetical and not the source document's —
  // some partners read positionally.
  await expectPreview(page, /"second"[\s\S]*"first"/);
});

// ─── The editor itself ────────────────────────────────────────────────────────

test("removing a field, a list, and a written entry", async ({ page }) => {
  await openWithSample(page);

  await addPathRule(page, "customer", "order.customer");
  await addList(page, "lines", "order.line");
  const lines = page.getByRole("group", { name: "Rules for the list lines" });
  await addListField(lines, "lines", "code", "sku");
  await lines.getByRole("button", { name: "Add an entry to lines" }).click();

  await expectPreview(page, '"code": "A1"');

  await page.getByRole("button", { name: "Remove entry 1" }).click();
  await expect(page.getByRole("group", { name: "Entry 1" })).toHaveCount(0);

  await page.getByRole("button", { name: "Remove the rule for code" }).click();
  await page.getByRole("button", { name: "Remove the list lines" }).click();
  await page.getByRole("button", { name: "Remove the rule for customer" }).click();

  await expect(page.getByText("No rules yet.")).toBeVisible();
  // Removing every rule is a mapping that produces an empty document, not a failure.
  await expectPreview(page, "{}");
});

test("undo puts back a rule that was removed", async ({ page }) => {
  await openWithSample(page);

  await addPathRule(page, "customer", "order.customer");
  await expectPreview(page, '"customer": "Ali"');

  await page.getByRole("button", { name: "Remove the rule for customer" }).click();
  await expect(page.getByRole("textbox", { name: "Output field name" })).toHaveCount(0);

  await page.getByRole("button", { name: "Undo" }).click();
  await expect(page.getByRole("textbox", { name: "Output field name" })).toHaveValue("customer");

  await page.getByRole("button", { name: "Redo" }).click();
  await expect(page.getByRole("textbox", { name: "Output field name" })).toHaveCount(0);
});

test("searching the output keeps the branches above a match", async ({ page }) => {
  await openWithSample(page, { c: "Amman", k: "JO", n: "Ali" });

  await addPathRule(page, "billing.city", "c");
  await addPathRule(page, "billing.country", "k");
  await addPathRule(page, "name", "n");

  await page.getByRole("textbox", { name: "Search output fields" }).fill("city");

  // The match is reachable, which means the object above it survives too.
  await expect(page.getByRole("group", { name: "Fields inside billing" })).toBeVisible();
  const names = page.getByRole("textbox", { name: "Output field name" });
  await expect(names).toHaveCount(1);
  await expect(names).toHaveValue("city");

  // Searching does not change the mapping — only what is shown of it.
  await expect(preview(page)).toContainText('"name": "Ali"');

  await page.getByRole("textbox", { name: "Search output fields" }).fill("");
  await expect(page.getByRole("textbox", { name: "Output field name" })).toHaveCount(3);
});

test("a list folds away without losing what is inside it", async ({ page }) => {
  await openWithSample(page);

  await addList(page, "lines", "order.line");
  const lines = page.getByRole("group", { name: "Rules for the list lines" });
  await addListField(lines, "lines", "code", "sku");
  await expectPreview(page, '"code": "A1"');

  await page.getByRole("button", { name: "Collapse the list lines" }).click();
  await expect(page.getByRole("group", { name: "Rules for the list lines" })).toHaveCount(0);
  // Folded, not removed: the mapping still produces the same document.
  await expect(preview(page)).toContainText('"code": "A1"');

  await page.getByRole("button", { name: "Expand the list lines" }).click();
  await expect(
    page.getByRole("group", { name: "Rules for the list lines" }).getByRole("textbox", {
      name: "Output field name",
    }),
  ).toHaveValue("code");
});

test("a rule with no name is reported rather than dropped", async ({ page }) => {
  await openWithSample(page);

  await page.getByRole("button", { name: "Add a field", exact: true }).click();
  await setSourcePath(page, "order.customer");

  // A value with nowhere to go is a mistake worth naming — the old mapper wrote it
  // to an empty key and moved on.
  await expect(page.getByText(/no target/).first()).toBeVisible({ timeout: 15000 });
});

test("how many rules there are, and how many have a value", async ({ page }) => {
  await openWithSample(page);

  await addPathRule(page, "customer", "order.customer");
  await expect(page.getByText("1 rule · 1 assigned")).toBeVisible();

  await page.getByRole("button", { name: "Add a field", exact: true }).click();
  await page.getByRole("textbox", { name: "Output field name" }).last().fill("pending");

  // Counted but not assigned, which is the difference between a mapping being
  // incomplete and being wrong.
  await expect(page.getByText("2 rules · 1 assigned")).toBeVisible();
});

// ─── Stored rules the editor must not open ────────────────────────────────────

const REFUSED = [
  {
    what: "are not readable at all",
    rules: "{ this is not json",
    says: /could not be read/,
  },
  {
    what: "come from a newer version of Bitween",
    rules: JSON.stringify({ version: 99, fields: [], lists: [] }),
    says: /version 99/,
  },
  {
    what: "were saved before lists were renamed",
    rules: JSON.stringify({ version: 1, fields: [], loops: [{ over: "x", target: ["y"] }] }),
    says: /before lists were renamed/,
  },
];

test("rules the editor cannot read refuse to open rather than starting blank", async ({ page }) => {
  const subscriptionId = await createSubscription(page);

  for (const { what, rules, says } of REFUSED) {
    await writeMapperProperties(subscriptionId, "NativeMapper", { MappingRules: rules });

    await page.goto(`subscriptions/${subscriptionId}/mapper`);

    // Opening blank and letting someone press Save would replace a working mapping
    // with nothing, which is worse than refusing to open.
    await expect(page.getByText(says), what).toBeVisible({ timeout: 15000 });
    await expect(page.getByRole("button", { name: "Save" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Back to the subscription" })).toBeVisible();
  }
});

test("a stored date format the dropdown never offered still shows what is saved", async ({
  page,
}) => {
  const subscriptionId = await createSubscription(page);

  // The engine formats with any .NET pattern, so a saved mapping can hold one this
  // closed list does not offer — set through the API, or offered here under a label
  // that has since changed. A select with no matching option shows nothing selected,
  // which reads as "no format chosen".
  await writeMapperProperties(subscriptionId, "NativeMapper", {
    MappingRules: JSON.stringify({
      version: 1,
      sourceFormat: "json",
      targetFormat: "json",
      fields: [
        {
          target: ["shipped"],
          from: { kind: "path", path: "order.date" },
          transform: { fn: "formatDate", format: "d MMMM" },
        },
      ],
      lists: [],
    }),
  });

  await page.goto(`subscriptions/${subscriptionId}/mapper`);
  await expect(page.getByRole("button", { name: "Save" })).toBeVisible({ timeout: 15000 });
  await openDetail(page, "shipped");

  await expect(page.getByLabel("Format a date — Format")).toHaveValue("d MMMM");

  // And saving the mapping for some unrelated reason must not quietly replace it.
  await addFixedRule(page, "channel", "web");
  await saveAndReload(page);
  await openDetail(page, "shipped");
  await expect(page.getByLabel("Format a date — Format")).toHaveValue("d MMMM");
});

test("the sample document is stored with the mapping, so it is there next time", async ({
  page,
}) => {
  await openWithSample(page);
  await addPathRule(page, "customer", "order.customer");
  await saveAndReload(page);

  // Reopening a mapping months later with no sample to hand meant it could not be
  // previewed at all, so the sample is part of what is saved.
  await expect(page.getByRole("textbox", { name: "Sample source document" })).toHaveValue(SAMPLE);
  await expectPreview(page, '"customer": "Ali"');
});

test("a source document that is not JSON says so instead of previewing nothing", async ({
  page,
}) => {
  await openWithSample(page, "{ not json at all");
  await addNamedRule(page, "customer");

  await expect(page.getByText(/could not be read|not valid/i).first()).toBeVisible({
    timeout: 15000,
  });
});

// ─── The toolbar, and reading a big mapping ───────────────────────────────────

test("clicking a row shows what is behind its chevron", async ({ page }) => {
  await openWithSample(page);

  await addPathRule(page, "total", "order.net");

  // The transform and the type live behind the chevron, and finding the chevron was
  // the whole complaint: the row itself is the obvious thing to click.
  await expect(page.getByRole("combobox", { name: "Transform" })).toHaveCount(0);

  await page.getByRole("textbox", { name: "Output field name" }).click();
  await expect(page.getByRole("combobox", { name: "Transform" })).toHaveCount(0);

  // Clicking the row's own space, rather than a control in it.
  await page.getByText("←", { exact: true }).first().click();
  await expect(page.getByRole("combobox", { name: "Transform" })).toBeVisible();

  await page.getByText("←", { exact: true }).first().click();
  await expect(page.getByRole("combobox", { name: "Transform" })).toHaveCount(0);
});

test("matching the source fields fills in the rules already there", async ({ page }) => {
  await openWithSample(page, {
    customer: "Ali",
    net: 100,
    line: [{ sku: "A1" }],
  });

  // Rules with names but no source — a mapping typed out from a partner's spec
  // before anyone had a sample document to point it at.
  await addNamedRule(page, "customer");
  await addNamedRule(page, "net");
  await addNamedRule(page, "somethingElse");

  await addList(page, "lines", "line");
  const lines = page.getByRole("group", { name: "Rules for the list lines" });
  await lines.getByRole("button", { name: "Add a field to lines" }).click();
  await lines.getByRole("textbox", { name: "Output field name" }).last().fill("sku");

  await page.getByRole("button", { name: "Match the source fields" }).click();

  await expect(page.getByText("3 matched · 1 left empty")).toBeVisible();
  // A rule inside a list is matched against one entry, so `sku` and not `line.sku`.
  await expect(lines.getByRole("combobox", { name: "Source field" })).toHaveValue("sku");

  await expectPreview(page, '"customer": "Ali"');
  await expect(preview(page)).toContainText('"sku": "A1"');

  // The one it could not place is empty rather than guessed at.
  const names = page.getByRole("combobox", { name: "Source field" });
  await expect(names.nth(2)).toHaveValue("");
});

test("clearing every rule, and undoing it", async ({ page }) => {
  await openWithSample(page);

  await addPathRule(page, "customer", "order.customer");
  await addList(page, "lines", "order.line");
  await expectPreview(page, '"customer": "Ali"');

  await page.getByRole("button", { name: "Clear all the rules" }).click();
  await expect(page.getByText("No rules yet.")).toBeVisible();

  // One press, one undo — not one per rule it removed.
  await page.getByRole("button", { name: "Undo" }).click();
  await expect(page.getByRole("textbox", { name: "Output field name" })).toHaveValue("customer");
  await expect(page.getByRole("textbox", { name: "Output list name" })).toHaveValue("lines");
  // The sample survives, because it was never a rule.
  await expect(page.getByRole("textbox", { name: "Sample source document" })).toHaveValue(SAMPLE);
});

test("hiding the preview gives the rules the whole width", async ({ page }) => {
  await openWithSample(page);
  await addPathRule(page, "customer", "order.customer");
  await expectPreview(page, '"customer": "Ali"');

  await page.getByRole("button", { name: "Hide the preview" }).click();
  await expect(page.getByText("— what a partner would receive")).toHaveCount(0);

  // Hidden, not switched off: the rules are untouched and it comes back as it was.
  await page.getByRole("button", { name: "Show the preview" }).click();
  await expectPreview(page, '"customer": "Ali"');
});

test("the partner key box offers the keys the previewed partner actually has", async ({ page }) => {
  await openWithSample(page, { order: { customer: "Ali" } });

  await addNamedRule(page, "warehouse");
  await page.getByRole("radio", { name: "Partner" }).last().click();

  const key = page.getByRole("combobox", { name: "Partner property key" });

  // With no partner chosen there is nothing to suggest, and the box is still a box:
  // the mapping runs against whichever partner the exchange belongs to, not this one.
  expect(await suggestionsFor(page, key)).toEqual([]);

  await previewAsTestPartner(page);
  // Fetched for the chosen partner, so the box fills in a moment rather than at once.
  await expect
    .poll(() => suggestionsFor(page, key))
    .toEqual(expect.arrayContaining(["WarehouseCode", "SenderId"]));

  await key.fill("WarehouseCode");
  await expectPreview(page, '"warehouse": "WH-7"');
  await expect(page.getByText("⚠")).toHaveCount(0);

  // A key that partner does not have is flagged rather than refused.
  await key.fill("NotAProperty");
  await expect(page.getByText("⚠")).toBeVisible();
});

// ─── The source side of a big document ────────────────────────────────────────

test("the source tree shows the fields inside a list, named as a rule names them", async ({
  page,
}) => {
  await openWithSample(page);

  // `sku` sits inside `order.line`, and a rule in a list over that list reads it as
  // `sku`. Hiding these meant the only way to see what was in a list was to read the
  // sample somewhere else.
  await expect(page.getByRole("button", { name: "sku", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "qty", exact: true })).toBeVisible();

  await page.getByRole("button", { name: "Collapse order.line" }).click();
  await expect(page.getByRole("button", { name: "sku", exact: true })).toHaveCount(0);

  await page.getByRole("button", { name: "Expand order.line" }).click();
  await expect(page.getByRole("button", { name: "sku", exact: true })).toBeVisible();
});

test("each object says how much of it is already read", async ({ page }) => {
  await openWithSample(page);

  // `order` holds customer, net, and the line's sku and qty.
  const order = page.getByRole("button", { name: "Collapse order", exact: true });
  await expect(order).toContainText("0/4");

  await addPathRule(page, "customerName", "order.customer");
  await expect(order).toContainText("1/4");

  // A rule inside a list counts too: it reads one of the fields in there.
  const lines = await addList(page, "lines", "order.line");
  await addListField(lines, "lines", "code", "sku");
  await expect(order).toContainText("2/4");
});

test("dragging a field out of a list onto a rule in that list wires it up", async ({ page }) => {
  await openWithSample(page);

  const lines = await addList(page, "lines", "order.line");
  await lines.getByRole("button", { name: "Add a field to lines" }).click();
  await lines.getByRole("textbox", { name: "Output field name" }).last().fill("code");

  await page
    .getByRole("button", { name: "sku", exact: true })
    .dragTo(lines.getByRole("textbox", { name: "Output field name" }));

  await expect(lines.getByRole("combobox", { name: "Source field" })).toHaveValue("sku");
  await expectPreview(page, '"code": "A1"');
});

test("undo, redo and save from the keyboard", async ({ page }) => {
  await openWithSample(page);

  await addPathRule(page, "customer", "order.customer");
  const source = page.getByRole("combobox", { name: "Source field" });
  await expect(source).toHaveValue("order.customer");

  // Focus is in a box after typing, and Ctrl+Z there belongs to the box. Clicking
  // the panel's own space takes it back.
  await page.getByText("Output", { exact: true }).click();

  // One step is one change, so this undoes pointing the rule somewhere — not the
  // whole rule, which was three changes ago.
  await page.keyboard.press("ControlOrMeta+z");
  await expect(source).toHaveValue("");

  await page.keyboard.press("ControlOrMeta+y");
  await expect(source).toHaveValue("order.customer");

  await page.keyboard.press("ControlOrMeta+z");
  await page.keyboard.press("ControlOrMeta+Shift+z");
  await expect(source).toHaveValue("order.customer");

  await page.keyboard.press("ControlOrMeta+s");
  await expect(page.getByText("Saved")).toBeVisible({ timeout: 15000 });
});

test("Ctrl+Z inside a box undoes the typing, not the mapping", async ({ page }) => {
  await openWithSample(page);
  await addPathRule(page, "customer", "order.customer");

  const name = page.getByRole("textbox", { name: "Output field name" });
  await name.click();
  await name.press("ControlOrMeta+z");

  // The rule is still there. The old editor took this key in both cases, so fixing a
  // mistyped name meant undoing a change somewhere else entirely.
  await expect(name).toHaveCount(1);
});

test("a checkbox in a settings panel can be ticked by its text", async ({ page }) => {
  await openWithSample(page);

  const lines = await addList(page, "lines", "order.line");
  await addListField(lines, "lines", "qty", "qty");
  await page.getByRole("button", { name: "Settings for the list lines" }).click();

  // Clicking the words, not the box — which is what anyone does, and what a test
  // using .check() never exercises, because that clicks the input directly. The row
  // click that opens these settings used to swallow it: the box ticked and the panel
  // folded away in the same tick, so it looked like the click did nothing.
  await page.getByText("Only some entries").click();

  await expect(page.getByRole("textbox", { name: "Filter field" })).toBeVisible();
  await page.getByRole("textbox", { name: "Filter field" }).fill("qty");
  await page.getByRole("combobox", { name: "Filter comparison" }).selectOption("greaterThan");
  await page.getByRole("textbox", { name: "Filter value" }).fill("0");

  // The entry with qty 0 is gone, which is the whole point of the checkbox.
  await expectPreview(page, '"qty": 2');
  await expect(preview(page)).not.toContainText('"qty": 0');
});

test("a checkbox in a rule's detail can be ticked by its text", async ({ page }) => {
  await openWithSample(page, { country: "JO" });

  await addPathRule(page, "countryName", "country");
  await openDetail(page, "countryName");

  await page.getByText("Substitute values from a table").click();
  await expect(page.getByRole("button", { name: "Add incoming value" })).toBeVisible();

  await page.getByRole("button", { name: "Add incoming value" }).click();
  await page.getByRole("textbox", { name: "Incoming value 1" }).fill("JO");
  await page.getByRole("textbox", { name: "Becomes 1" }).fill("Jordan");

  await expectPreview(page, '"countryName": "Jordan"');
});

test("a date that could be read two ways has to say which", async ({ page }) => {
  // A real CargoNet shipping date: the 4th of September, French style.
  await openWithSample(page, { order: { shippingdate: "04.09.2026" } });

  await addPathRule(page, "shipDate", "order.shippingdate");
  await openDetail(page, "shipDate");
  await page.getByRole("combobox", { name: "Transform" }).selectOption("formatDate");

  // One control on the row, and it is a closed list: what the date should look like on
  // the way out, shown as the date itself rather than as yyyy-MM-dd letters.
  const format = page.getByRole("combobox", { name: "Format a date — Format" });
  await expect(format.locator("option")).toContainText(["Format…", "2026-09-04", "04/09/2026"]);
  await format.selectOption("yyyy-MM-dd");

  // Refused rather than guessed. The invariant parser reads this as the 9th of April
  // perfectly happily, which would date a shipment five months out with nothing said.
  await expect(page.getByText(/could not read '04\.09\.2026'/).first()).toBeVisible({
    timeout: 15000,
  });

  // Answered once for the document, under the sample it describes — a partner writes
  // dates one way throughout, so this is not a per-rule question.
  const dates = page.getByRole("combobox", { name: "Dates in the incoming document" });
  await expect(dates.locator("option")).toHaveText([
    "Year first — 2026-09-04",
    "Day first — 04.09.2026",
    "Month first — 09.04.2026",
  ]);

  await dates.selectOption("dayFirst");
  await expectPreview(page, '"shipDate": "2026-09-04"');

  // And the other way round, from the same characters.
  await dates.selectOption("monthFirst");
  await expectPreview(page, '"shipDate": "2026-04-09"');

  // It is part of the mapping, so it comes back with it.
  await saveAndReload(page);
  await expect(page.getByRole("combobox", { name: "Dates in the incoming document" })).toHaveValue(
    "monthFirst",
  );
});
