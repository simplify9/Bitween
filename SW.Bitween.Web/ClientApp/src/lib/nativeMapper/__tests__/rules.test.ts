import { describe, expect, it } from "vitest";
import { fromWire, loadMapping, saveMapping, toWire } from "../serialize";
import {
  everyFieldRule,
  initialRulesEditorState,
  isAssigned,
  rulesEditorReducer,
  type RulesEditorAction,
  type RulesEditorState,
} from "../rulesReducer";
import {
  MAPPING_RULES_KEY,
  RULES_VERSION,
  SOURCE_SAMPLE_KEY,
  TRANSFORMS,
  emptyRules,
  type EditorRules,
  type MappingRules,
} from "../types";

const run = (actions: RulesEditorAction[], from = initialRulesEditorState): RulesEditorState =>
  actions.reduce(rulesEditorReducer, from);

const loaded = (rules: EditorRules): RulesEditorState =>
  run([{ type: "LOAD", rules, sourceSample: "", targetSample: "" }]);

// ─── The round trip ───────────────────────────────────────────────────────────

describe("saving and loading", () => {
  /**
   * The thing the old design got wrong. It saved a generated template and had to
   * reverse-engineer the rules back out of it; here what comes back is what went in.
   */
  it("reloads exactly what it saved", () => {
    const wire: MappingRules = {
      version: RULES_VERSION,
      sourceFormat: "json",
      targetFormat: "json",
      fields: [
        {
          target: ["customer", "name"],
          from: { kind: "path", path: "order.customer" },
          transform: { fn: "upper" },
          type: "string",
        },
        { target: ["region"], from: { kind: "partner", key: "region-code" } },
        {
          target: ["state"],
          from: { kind: "path", path: "order.state" },
          lookup: { table: { NEW: 1 }, fallback: 0 },
          type: "number",
        },
      ],
      loops: [
        {
          over: "order.line",
          as: "line",
          target: ["lines"],
          where: { field: "qty", operator: "greaterThan", value: 0 },
          fields: [{ target: ["sku"], from: { kind: "path", path: "sku" } }],
          loops: [
            { over: "tags", target: ["labels"], item: { target: [], from: { kind: "path", path: "t" } }, fields: [], loops: [] },
          ],
        },
      ],
    };

    expect(toWire(fromWire(wire))).toEqual(wire);
  });

  it("survives a trip through the stored properties", () => {
    const original = fromWire({
      version: RULES_VERSION,
      sourceFormat: "json",
      targetFormat: "json",
      fields: [{ target: ["a"], from: { kind: "fixed", value: "WEB" } }],
      loops: [],
    });

    const stored = saveMapping(original, '{"x":1}', "{}");
    const back = loadMapping(stored);

    expect(back.error).toBeUndefined();
    expect(toWire(back.rules)).toEqual(toWire(original));
    expect(back.sourceSample).toBe('{"x":1}');
  });

  it("keeps a dotted key as one segment", () => {
    const stored = saveMapping(
      fromWire({
        version: RULES_VERSION,
        sourceFormat: "json",
        targetFormat: "json",
        fields: [{ target: ["file.txt"], from: { kind: "fixed", value: 1 } }],
        loops: [],
      }),
      "",
      "",
    );

    expect(loadMapping(stored).rules.fields[0].target).toEqual(["file.txt"]);
  });

  it("never writes editor ids to the wire", () => {
    const wire = toWire(
      fromWire({
        version: RULES_VERSION,
        sourceFormat: "json",
        targetFormat: "json",
        fields: [{ target: ["a"], from: { kind: "fixed", value: 1 } }],
        loops: [
          { over: "x", target: ["y"], fields: [{ target: ["b"], from: { kind: "fixed", value: 2 } }], loops: [] },
        ],
      }),
    );

    expect(JSON.stringify(wire)).not.toContain('"id"');
  });

  it("gives every rule its own id", () => {
    const rules = fromWire({
      version: RULES_VERSION,
      sourceFormat: "json",
      targetFormat: "json",
      fields: [
        { target: ["a"], from: { kind: "fixed", value: 1 } },
        { target: ["b"], from: { kind: "fixed", value: 2 } },
      ],
      loops: [],
    });

    expect(rules.fields[0].id).not.toBe(rules.fields[1].id);
  });

  it("starts blank when there is nothing stored", () => {
    const back = loadMapping({});

    expect(back.error).toBeUndefined();
    expect(back.rules.fields).toEqual([]);
    expect(back.rules.version).toBe(RULES_VERSION);
  });

  /**
   * Reported rather than swallowed. Opening blank over rules that were merely
   * unparseable would let the next save destroy them.
   */
  it("reports unreadable rules instead of starting blank", () => {
    const back = loadMapping({ [MAPPING_RULES_KEY]: "{ not json" });

    expect(back.error).toMatch(/could not be read/);
  });

  it("refuses rules from a newer version", () => {
    const back = loadMapping({
      [MAPPING_RULES_KEY]: JSON.stringify({ version: 99, fields: [] }),
    });

    expect(back.error).toMatch(/version 99/);
  });

  it("keeps the samples even when the rules are unreadable", () => {
    const back = loadMapping({
      [MAPPING_RULES_KEY]: "{ not json",
      [SOURCE_SAMPLE_KEY]: '{"kept":true}',
    });

    expect(back.sourceSample).toBe('{"kept":true}');
  });
});

// ─── Editing ──────────────────────────────────────────────────────────────────

describe("editing rules", () => {
  it("adds a top-level field and selects it", () => {
    const state = run([{ type: "ADD_FIELD", loopId: null, target: ["a"] }]);

    expect(state.rules.fields).toHaveLength(1);
    expect(state.selectedId).toBe(state.rules.fields[0].id);
    expect(state.dirty).toBe(true);
  });

  it("updates a field by id", () => {
    let state = run([{ type: "ADD_FIELD", loopId: null, target: ["a"] }]);
    const id = state.rules.fields[0].id;

    state = rulesEditorReducer(state, {
      type: "UPDATE_FIELD",
      id,
      changes: { from: { kind: "fixed", value: "WEB" }, type: "string" },
    });

    expect(state.rules.fields[0].from).toEqual({ kind: "fixed", value: "WEB" });
    expect(state.rules.fields[0].type).toBe("string");
  });

  /**
   * Switching where a value comes from replaces the whole source object, so two
   * sources cannot both be set. The old model had six optional fields and picked
   * whichever happened to be filled, which is what its mode-switching bugs were.
   */
  it("switching source kind leaves nothing behind", () => {
    let state = run([{ type: "ADD_FIELD", loopId: null, target: ["a"] }]);
    const id = state.rules.fields[0].id;

    state = rulesEditorReducer(state, {
      type: "UPDATE_FIELD",
      id,
      changes: { from: { kind: "path", path: "order.customer" } },
    });
    state = rulesEditorReducer(state, {
      type: "UPDATE_FIELD",
      id,
      changes: { from: { kind: "partner", key: "region" } },
    });

    expect(state.rules.fields[0].from).toEqual({ kind: "partner", key: "region" });
    expect(state.rules.fields[0].from.path).toBeUndefined();
  });

  it("removes a field and clears the selection", () => {
    let state = run([{ type: "ADD_FIELD", loopId: null, target: ["a"] }]);
    const id = state.rules.fields[0].id;

    state = rulesEditorReducer(state, { type: "REMOVE_FIELD", id });

    expect(state.rules.fields).toHaveLength(0);
    expect(state.selectedId).toBeNull();
  });

  it("adds a field inside a loop", () => {
    let state = run([{ type: "ADD_LOOP", parentLoopId: null, target: ["lines"] }]);
    const loopId = state.rules.loops[0].id;

    state = rulesEditorReducer(state, { type: "ADD_FIELD", loopId, target: ["sku"] });

    expect(state.rules.loops[0].fields).toHaveLength(1);
    expect(state.rules.fields).toHaveLength(0);
  });

  it("nests a loop inside a loop and updates the inner one", () => {
    let state = run([{ type: "ADD_LOOP", parentLoopId: null, target: ["orders"] }]);
    const outer = state.rules.loops[0].id;

    state = rulesEditorReducer(state, { type: "ADD_LOOP", parentLoopId: outer, target: ["items"] });
    const inner = state.rules.loops[0].loops[0].id;

    state = rulesEditorReducer(state, { type: "UPDATE_LOOP", id: inner, changes: { over: "lines" } });

    expect(state.rules.loops[0].loops[0].over).toBe("lines");
  });

  it("removes a nested loop without touching its parent", () => {
    let state = run([{ type: "ADD_LOOP", parentLoopId: null, target: ["orders"] }]);
    const outer = state.rules.loops[0].id;
    state = rulesEditorReducer(state, { type: "ADD_LOOP", parentLoopId: outer, target: ["items"] });
    const inner = state.rules.loops[0].loops[0].id;

    state = rulesEditorReducer(state, { type: "REMOVE_LOOP", id: inner });

    expect(state.rules.loops).toHaveLength(1);
    expect(state.rules.loops[0].loops).toHaveLength(0);
  });

  it("turns the root into a list and back", () => {
    let state = run([{ type: "SET_ROOT_LOOP", enabled: true }]);
    expect(state.rules.root).toBeDefined();

    state = rulesEditorReducer(state, { type: "SET_ROOT_LOOP", enabled: false });
    expect(state.rules.root).toBeUndefined();
  });

  it("finds a field inside the root loop", () => {
    let state = run([{ type: "SET_ROOT_LOOP", enabled: true }]);
    const rootId = state.rules.root!.id;

    state = rulesEditorReducer(state, { type: "ADD_FIELD", loopId: rootId, target: ["code"] });
    const fieldId = state.rules.root!.fields[0].id;

    state = rulesEditorReducer(state, {
      type: "UPDATE_FIELD",
      id: fieldId,
      changes: { from: { kind: "path", path: "sku" } },
    });

    expect(state.rules.root!.fields[0].from.path).toBe("sku");
  });
});

// ─── Undo ─────────────────────────────────────────────────────────────────────

describe("undo and redo", () => {
  it("undoes a change to the mapping", () => {
    let state = run([{ type: "ADD_FIELD", loopId: null, target: ["a"] }]);
    state = rulesEditorReducer(state, { type: "UNDO" });

    expect(state.rules.fields).toHaveLength(0);
  });

  it("redoes it", () => {
    let state = run([{ type: "ADD_FIELD", loopId: null, target: ["a"] }]);
    state = rulesEditorReducer(state, { type: "UNDO" });
    state = rulesEditorReducer(state, { type: "REDO" });

    expect(state.rules.fields).toHaveLength(1);
  });

  /** Selecting a row is not an edit, so it must not consume an undo step. */
  it("selection does not go on the undo stack", () => {
    let state = run([{ type: "ADD_FIELD", loopId: null, target: ["a"] }]);
    const before = state.past.length;

    state = rulesEditorReducer(state, { type: "SELECT", id: null });

    expect(state.past.length).toBe(before);
  });

  it("undo on a fresh editor does nothing", () => {
    expect(rulesEditorReducer(initialRulesEditorState, { type: "UNDO" }).rules.fields).toEqual([]);
  });

  it("loading clears the history", () => {
    let state = run([{ type: "ADD_FIELD", loopId: null, target: ["a"] }]);
    state = rulesEditorReducer(state, {
      type: "LOAD",
      rules: emptyRules(),
      sourceSample: "",
      targetSample: "",
    });

    expect(state.past).toEqual([]);
    expect(state.dirty).toBe(false);
  });
});

// ─── Reading the state ────────────────────────────────────────────────────────

describe("reading the state", () => {
  it("lists every field rule including ones inside nested loops", () => {
    const rules = fromWire({
      version: RULES_VERSION,
      sourceFormat: "json",
      targetFormat: "json",
      fields: [{ target: ["a"], from: { kind: "fixed", value: 1 } }],
      loops: [
        {
          over: "x",
          target: ["y"],
          fields: [{ target: ["b"], from: { kind: "fixed", value: 2 } }],
          loops: [
            {
              over: "z",
              target: ["w"],
              item: { target: [], from: { kind: "path", path: "t" } },
              fields: [],
              loops: [],
            },
          ],
        },
      ],
    });

    const targets = everyFieldRule(rules).map((f) => f.rule.target.join("."));

    expect(targets).toHaveLength(3);
    expect(targets).toContain("a");
    expect(targets).toContain("b");
  });

  it("knows whether a rule has a value assigned", () => {
    const state = loaded(
      fromWire({
        version: RULES_VERSION,
        sourceFormat: "json",
        targetFormat: "json",
        fields: [
          { target: ["a"], from: { kind: "path", path: "" } },
          { target: ["b"], from: { kind: "path", path: "x" } },
          { target: ["c"], from: { kind: "fixed", value: "WEB" } },
          { target: ["d"], from: { kind: "partner", key: "" } },
          { target: ["e"], from: { kind: "global", setId: "s", key: "k" } },
          { target: ["f"], from: { kind: "global", setId: "s" } },
        ],
        loops: [],
      }),
    );

    expect(state.rules.fields.map(isAssigned)).toEqual([false, true, true, false, true, false]);
  });

  it("records preview errors per rule", () => {
    const state = run([
      {
        type: "PREVIEW_RESULT",
        output: null,
        ruleErrors: { total: "cannot convert 'abc' to number" },
        error: null,
      },
    ]);

    expect(state.ruleErrors.total).toMatch(/cannot convert/);
  });
});

// ─── The transform list ───────────────────────────────────────────────────────

describe("transforms offered to the user", () => {
  /**
   * Mirrors Transforms.Names in C#, which has its own test asserting every listed
   * name is implemented. A name here that C# does not know would be a dropdown
   * entry that fails at runtime.
   */
  it("matches the functions the mapper implements", () => {
    expect(TRANSFORMS.map((t) => t.fn).sort()).toEqual(
      [
        "add",
        "concat",
        "defaultIfEmpty",
        "formatDate",
        "lower",
        "multiply",
        "replace",
        "round",
        "substring",
        "trim",
        "upper",
      ].sort(),
    );
  });

  it("gives every function a label and named arguments", () => {
    for (const transform of TRANSFORMS) {
      expect(transform.label).toBeTruthy();
      for (const arg of transform.args) {
        expect(arg.name).toBeTruthy();
        expect(arg.label).toBeTruthy();
      }
    }
  });
});
