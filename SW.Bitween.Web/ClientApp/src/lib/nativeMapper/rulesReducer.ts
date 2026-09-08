import { produce } from "immer";
import {
  emptyFieldRule,
  emptyLoopRule,
  emptyRules,
  type DocumentFormatId,
  type EditorFieldRule,
  type EditorLoopRule,
  type EditorRules,
  type RuleId,
} from "./types";

// ─── Editor state ─────────────────────────────────────────────────────────────

export interface RulesEditorState {
  rules: EditorRules;
  sourceSample: string;
  targetSample: string;
  /** Which rule row is selected, for the connection canvas and the detail panel. */
  selectedId: RuleId | null;
  hoveredPath: string | null;
  searchSource: string;
  searchTarget: string;
  /** Rules that failed in the last preview, by target path. */
  ruleErrors: Record<string, string>;
  /** Set when the mapping could not be previewed at all. */
  previewError: string | null;
  previewOutput: string | null;
  /** Set when stored rules could not be read, which blocks editing. */
  loadError: string | null;
  dirty: boolean;
  past: EditorRules[];
  future: EditorRules[];
}

export const initialRulesEditorState: RulesEditorState = {
  rules: emptyRules(),
  sourceSample: "",
  targetSample: "",
  selectedId: null,
  hoveredPath: null,
  searchSource: "",
  searchTarget: "",
  ruleErrors: {},
  previewError: null,
  previewOutput: null,
  loadError: null,
  dirty: false,
  past: [],
  future: [],
};

export type RulesEditorAction =
  | { type: "LOAD"; rules: EditorRules; sourceSample: string; targetSample: string; error?: string }
  | { type: "SET_SOURCE_FORMAT"; format: DocumentFormatId }
  | { type: "SET_TARGET_FORMAT"; format: DocumentFormatId }
  | { type: "SET_SOURCE_SAMPLE"; text: string }
  | { type: "SET_TARGET_SAMPLE"; text: string }
  | { type: "ADD_FIELD"; loopId: RuleId | null; target?: string[] }
  | { type: "UPDATE_FIELD"; id: RuleId; changes: Partial<Omit<EditorFieldRule, "id">> }
  | { type: "REMOVE_FIELD"; id: RuleId }
  | { type: "ADD_LOOP"; parentLoopId: RuleId | null; target?: string[] }
  | { type: "UPDATE_LOOP"; id: RuleId; changes: Partial<Omit<EditorLoopRule, "id" | "fields" | "loops">> }
  | { type: "REMOVE_LOOP"; id: RuleId }
  | { type: "SET_ROOT_LOOP"; enabled: boolean }
  | { type: "SELECT"; id: RuleId | null }
  | { type: "HOVER_PATH"; path: string | null }
  | { type: "SET_SEARCH_SOURCE"; text: string }
  | { type: "SET_SEARCH_TARGET"; text: string }
  | { type: "PREVIEW_RESULT"; output: string | null; ruleErrors: Record<string, string>; error: string | null }
  | { type: "SAVED" }
  | { type: "UNDO" }
  | { type: "REDO" };

// ─── Finding a rule ───────────────────────────────────────────────────────────
//
// Rules are addressed by id rather than by a path of indices. Loops nest, so an
// index path would have to be rebuilt every time a row moved — which is the kind
// of bookkeeping the old model needed three recursive helpers for.

/** Every loop in the tree, outermost first, including the root loop. */
function allLoops(rules: EditorRules): EditorLoopRule[] {
  const found: EditorLoopRule[] = [];
  const walk = (loops: EditorLoopRule[]) => {
    for (const loop of loops) {
      found.push(loop);
      walk(loop.loops);
    }
  };
  if (rules.root) walk([rules.root]);
  walk(rules.loops);
  return found;
}

/** The loop a rule lives in, or null when it is a top-level field. */
function findFieldOwner(
  rules: EditorRules,
  id: RuleId,
): { fields: EditorFieldRule[] } | null {
  if (rules.fields.some((f) => f.id === id)) return rules;
  for (const loop of allLoops(rules)) {
    if (loop.fields.some((f) => f.id === id)) return loop;
    if (loop.item?.id === id) return null; // item rules are updated through their loop
  }
  return null;
}

function findField(rules: EditorRules, id: RuleId): EditorFieldRule | undefined {
  if (rules.root?.item?.id === id) return rules.root.item;
  const top = rules.fields.find((f) => f.id === id);
  if (top) return top;
  for (const loop of allLoops(rules)) {
    const inLoop = loop.fields.find((f) => f.id === id);
    if (inLoop) return inLoop;
    if (loop.item?.id === id) return loop.item;
  }
  return undefined;
}

function findLoop(rules: EditorRules, id: RuleId): EditorLoopRule | undefined {
  return allLoops(rules).find((l) => l.id === id);
}

/** The list a loop lives in, so it can be removed from it. */
function findLoopSiblings(rules: EditorRules, id: RuleId): EditorLoopRule[] | null {
  if (rules.loops.some((l) => l.id === id)) return rules.loops;
  for (const loop of allLoops(rules)) {
    if (loop.loops.some((l) => l.id === id)) return loop.loops;
  }
  return null;
}

// ─── Reducer ──────────────────────────────────────────────────────────────────

const HISTORY_LIMIT = 50;

/** Whether an action changes the mapping itself, as opposed to what is selected. */
function changesTheMapping(action: RulesEditorAction): boolean {
  switch (action.type) {
    case "SET_SOURCE_FORMAT":
    case "SET_TARGET_FORMAT":
    case "ADD_FIELD":
    case "UPDATE_FIELD":
    case "REMOVE_FIELD":
    case "ADD_LOOP":
    case "UPDATE_LOOP":
    case "REMOVE_LOOP":
    case "SET_ROOT_LOOP":
      return true;
    default:
      return false;
  }
}

export function rulesEditorReducer(
  state: RulesEditorState,
  action: RulesEditorAction,
): RulesEditorState {
  return produce(state, (draft) => {
    if (changesTheMapping(action)) {
      draft.past = [...draft.past.slice(-(HISTORY_LIMIT - 1)), state.rules];
      draft.future = [];
      draft.dirty = true;
    }

    switch (action.type) {
      case "LOAD":
        draft.rules = action.rules;
        draft.sourceSample = action.sourceSample;
        draft.targetSample = action.targetSample;
        draft.loadError = action.error ?? null;
        draft.selectedId = null;
        draft.ruleErrors = {};
        draft.previewError = null;
        draft.previewOutput = null;
        draft.dirty = false;
        draft.past = [];
        draft.future = [];
        break;

      case "SET_SOURCE_FORMAT":
        draft.rules.sourceFormat = action.format;
        break;

      case "SET_TARGET_FORMAT":
        draft.rules.targetFormat = action.format;
        break;

      // Samples are an editor convenience — runtime never reads them — so changing
      // one is not a change to the mapping and does not go on the undo stack.
      case "SET_SOURCE_SAMPLE":
        draft.sourceSample = action.text;
        draft.dirty = true;
        break;

      case "SET_TARGET_SAMPLE":
        draft.targetSample = action.text;
        draft.dirty = true;
        break;

      case "ADD_FIELD": {
        const rule = emptyFieldRule(action.target ?? []);
        if (action.loopId === null) draft.rules.fields.push(rule);
        else findLoop(draft.rules, action.loopId)?.fields.push(rule);
        draft.selectedId = rule.id;
        break;
      }

      case "UPDATE_FIELD": {
        const rule = findField(draft.rules, action.id);
        if (rule) Object.assign(rule, action.changes);
        break;
      }

      case "REMOVE_FIELD": {
        const owner = findFieldOwner(draft.rules, action.id);
        if (owner) owner.fields = owner.fields.filter((f) => f.id !== action.id);
        if (draft.selectedId === action.id) draft.selectedId = null;
        break;
      }

      case "ADD_LOOP": {
        const loop = emptyLoopRule(action.target ?? []);
        if (action.parentLoopId === null) draft.rules.loops.push(loop);
        else findLoop(draft.rules, action.parentLoopId)?.loops.push(loop);
        draft.selectedId = loop.id;
        break;
      }

      case "UPDATE_LOOP": {
        const loop = findLoop(draft.rules, action.id);
        if (loop) Object.assign(loop, action.changes);
        break;
      }

      case "REMOVE_LOOP": {
        if (draft.rules.root?.id === action.id) {
          draft.rules.root = undefined;
        } else {
          const siblings = findLoopSiblings(draft.rules, action.id);
          if (siblings) {
            const at = siblings.findIndex((l) => l.id === action.id);
            if (at !== -1) siblings.splice(at, 1);
          }
        }
        if (draft.selectedId === action.id) draft.selectedId = null;
        break;
      }

      // A document is either an object with fields in it or a bare array, so turning
      // the root into a list puts the field and loop rules aside rather than trying to
      // keep both — and turning it off brings them back.
      case "SET_ROOT_LOOP":
        if (action.enabled && !draft.rules.root) draft.rules.root = emptyLoopRule();
        else if (!action.enabled) draft.rules.root = undefined;
        break;

      case "SELECT":
        draft.selectedId = action.id;
        break;

      case "HOVER_PATH":
        draft.hoveredPath = action.path;
        break;

      case "SET_SEARCH_SOURCE":
        draft.searchSource = action.text;
        break;

      case "SET_SEARCH_TARGET":
        draft.searchTarget = action.text;
        break;

      case "PREVIEW_RESULT":
        draft.previewOutput = action.output;
        draft.ruleErrors = action.ruleErrors;
        draft.previewError = action.error;
        break;

      case "SAVED":
        draft.dirty = false;
        break;

      case "UNDO": {
        if (state.past.length === 0) break;
        draft.future = [state.rules, ...state.future.slice(0, HISTORY_LIMIT - 1)];
        draft.rules = state.past[state.past.length - 1];
        draft.past = state.past.slice(0, -1);
        draft.dirty = true;
        break;
      }

      case "REDO": {
        if (state.future.length === 0) break;
        draft.past = [...state.past.slice(-(HISTORY_LIMIT - 1)), state.rules];
        draft.rules = state.future[0];
        draft.future = state.future.slice(1);
        draft.dirty = true;
        break;
      }
    }
  });
}

// ─── Reading the state ────────────────────────────────────────────────────────

/** The target path of a rule, as the preview endpoint reports it in an error. */
export function targetPathOf(target: string[]): string {
  return target.length === 0 ? "(no target)" : target.join(".");
}

/** Every field rule in the mapping, with the loop it belongs to. */
export function everyFieldRule(
  rules: EditorRules,
): { rule: EditorFieldRule; loop: EditorLoopRule | null }[] {
  const out: { rule: EditorFieldRule; loop: EditorLoopRule | null }[] = [];
  for (const rule of rules.fields) out.push({ rule, loop: null });
  for (const loop of allLoops(rules)) {
    if (loop.item) out.push({ rule: loop.item, loop });
    for (const rule of loop.fields) out.push({ rule, loop });
  }
  return out;
}

/** Whether a rule has a value assigned, for the "n of m assigned" count. */
export function isAssigned(rule: EditorFieldRule): boolean {
  switch (rule.from.kind) {
    case "path":
      return Boolean(rule.from.path?.trim());
    case "fixed":
      return rule.from.value !== undefined && rule.from.value !== "";
    case "partner":
      return Boolean(rule.from.key?.trim());
    case "global":
      return Boolean(rule.from.setId?.trim() && rule.from.key?.trim());
    default:
      return false;
  }
}

export { allLoops };
