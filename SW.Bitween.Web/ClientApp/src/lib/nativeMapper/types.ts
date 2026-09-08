// ─── The rules a NativeMapper mapping is made of ─────────────────────────────
//
// These mirror SW.Bitween.NativeAdapters.Mapper.MappingRules. The wire format is
// pinned from the other side by RulesSerializationTests.cs: camelCase property
// names and lowercase enum values, so this file can read the way TypeScript
// normally does rather than mirroring C# spelling.
//
// Unlike the old mapper, this is what gets saved. Nothing is generated from it and
// nothing is recovered out of anything, so the editor reads back exactly what it
// wrote.

export type DocumentFormatId = "json";

export type ValueSourceKind = "path" | "fixed" | "partner" | "global";

export type ValueTypeName = "string" | "number" | "boolean";

export type FilterOperatorName =
  | "equal"
  | "notEqual"
  | "greaterThan"
  | "greaterThanOrEqual"
  | "lessThan"
  | "lessThanOrEqual";

/** Where a field's value comes from. Exactly one shape, chosen by `kind`. */
export interface ValueSource {
  kind: ValueSourceKind;
  /** `path` — dot-separated path into the source document. */
  path?: string;
  /** `fixed` — a literal. */
  value?: unknown;
  /** `partner` — an adapter-property key. `global` — a key within `setId`. */
  key?: string;
  /** `global` — which values set. */
  setId?: string;
}

/** A named function and its arguments, e.g. `{ fn: "multiply", by: 1.16 }`. */
export interface TransformRule {
  fn: string;
  [arg: string]: unknown;
}

export interface LookupRule {
  table: Record<string, unknown>;
  /** Used when the value is not a key in `table`. Absent means output null. */
  fallback?: unknown;
}

export interface FilterRule {
  field: string;
  operator: FilterOperatorName;
  value?: unknown;
}

export interface FieldRule {
  /** Path segments, so a key may contain a dot. */
  target: string[];
  from: ValueSource;
  transform?: TransformRule;
  lookup?: LookupRule;
  /** Absent leaves the value as the source produced it. */
  type?: ValueTypeName;
}

export interface LoopRule {
  /** Path to the list to walk. Empty means the document itself, for a root array. */
  over: string;
  /** What the item is called, for the reader's benefit. */
  as?: string;
  target: string[];
  where?: FilterRule;
  /** Set for a list of plain values instead of a list of records. */
  item?: FieldRule;
  fields: FieldRule[];
  loops: LoopRule[];
}

export interface MappingRules {
  version: number;
  sourceFormat: DocumentFormatId;
  targetFormat: DocumentFormatId;
  fields: FieldRule[];
  loops: LoopRule[];
  /** When set, the whole output is this list rather than an object. */
  root?: LoopRule;
}

export const RULES_VERSION = 1;

export const NATIVE_MAPPER_ID = "NativeMapper";

/** The key the rules are stored under in a subscription's mapper properties. */
export const MAPPING_RULES_KEY = "MappingRules";
export const SOURCE_SAMPLE_KEY = "SourceSample";
export const TARGET_SAMPLE_KEY = "TargetSample";

// ─── Editor-side model ────────────────────────────────────────────────────────
//
// The same rules with a client-side id on each one, so React can key rows and the
// reducer can address a rule without walking indices through nested loops. Ids are
// added on load and stripped on save — they never reach the wire.

export type RuleId = string;

export interface EditorFieldRule extends FieldRule {
  id: RuleId;
}

export interface EditorLoopRule extends Omit<LoopRule, "fields" | "loops" | "item"> {
  id: RuleId;
  item?: EditorFieldRule;
  fields: EditorFieldRule[];
  loops: EditorLoopRule[];
}

export interface EditorRules extends Omit<MappingRules, "fields" | "loops" | "root"> {
  fields: EditorFieldRule[];
  loops: EditorLoopRule[];
  root?: EditorLoopRule;
}

let nextId = 0;

export const newRuleId = (): RuleId => `r${++nextId}`;

/** A field rule with nothing assigned yet, for a row the user has just added. */
export const emptyFieldRule = (target: string[] = []): EditorFieldRule => ({
  id: newRuleId(),
  target,
  from: { kind: "path", path: "" },
});

export const emptyLoopRule = (target: string[] = []): EditorLoopRule => ({
  id: newRuleId(),
  over: "",
  target,
  fields: [],
  loops: [],
});

export const emptyRules = (): EditorRules => ({
  version: RULES_VERSION,
  sourceFormat: "json",
  targetFormat: "json",
  fields: [],
  loops: [],
});

/** The formats the mapper can read and write, for the dropdowns. */
export const DOCUMENT_FORMATS: { id: DocumentFormatId; label: string }[] = [
  { id: "json", label: "JSON" },
];

/** Filter operators, with the symbol a user recognises. */
export const FILTER_OPERATORS: { value: FilterOperatorName; label: string }[] = [
  { value: "equal", label: "=" },
  { value: "notEqual", label: "≠" },
  { value: "greaterThan", label: ">" },
  { value: "greaterThanOrEqual", label: "≥" },
  { value: "lessThan", label: "<" },
  { value: "lessThanOrEqual", label: "≤" },
];

/**
 * The transform functions and the arguments each takes.
 *
 * Mirrors Transforms.Names in C#, which has a test asserting every listed name is
 * implemented. A name here that C# does not know would be a dropdown entry that
 * fails at runtime.
 */
export const TRANSFORMS: {
  fn: string;
  label: string;
  args: { name: string; label: string; kind: "text" | "number" }[];
}[] = [
  { fn: "upper", label: "Uppercase", args: [] },
  { fn: "lower", label: "Lowercase", args: [] },
  { fn: "trim", label: "Trim spaces", args: [] },
  {
    fn: "substring",
    label: "Take part of the text",
    args: [
      { name: "start", label: "Start at", kind: "number" },
      { name: "length", label: "Length", kind: "number" },
    ],
  },
  {
    fn: "replace",
    label: "Replace text",
    args: [
      { name: "find", label: "Find", kind: "text" },
      { name: "with", label: "Replace with", kind: "text" },
    ],
  },
  { fn: "concat", label: "Append text", args: [{ name: "with", label: "Append", kind: "text" }] },
  { fn: "round", label: "Round", args: [{ name: "decimals", label: "Decimals", kind: "number" }] },
  { fn: "multiply", label: "Multiply", args: [{ name: "by", label: "By", kind: "number" }] },
  { fn: "add", label: "Add", args: [{ name: "amount", label: "Amount", kind: "number" }] },
  {
    fn: "formatDate",
    label: "Format a date",
    args: [{ name: "format", label: "Format", kind: "text" }],
  },
  {
    fn: "defaultIfEmpty",
    label: "Use a default when empty",
    args: [{ name: "value", label: "Default", kind: "text" }],
  },
];
