import {
  MAPPING_RULES_KEY,
  RULES_VERSION,
  SOURCE_SAMPLE_KEY,
  TARGET_SAMPLE_KEY,
  emptyRules,
  newRuleId,
  type DocumentFormatId,
  type EditorFieldRule,
  type EditorLoopRule,
  type EditorRules,
  type FieldRule,
  type LoopRule,
  type MappingRules,
} from "./types";

// ─── Between the editor and the wire ──────────────────────────────────────────
//
// The only translation in the whole mapper, and it is a trivial one: add a client
// id on the way in, drop it on the way out. Everything else is the same object.
//
// That is the difference from the old mapper, which converted its state into a
// Scriban template on save and had to reverse-engineer it with 338 lines of
// regular expressions on load.

const withFieldIds = (fields: FieldRule[] | undefined): EditorFieldRule[] =>
  (fields ?? []).map((f) => ({ ...f, id: newRuleId() }));

const withLoopIds = (loops: LoopRule[] | undefined): EditorLoopRule[] =>
  (loops ?? []).map(loopWithIds);

function loopWithIds(loop: LoopRule): EditorLoopRule {
  return {
    ...loop,
    id: newRuleId(),
    item: loop.item ? { ...loop.item, id: newRuleId() } : undefined,
    fields: withFieldIds(loop.fields),
    loops: withLoopIds(loop.loops),
  };
}

const stripFieldId = ({ id: _id, ...rest }: EditorFieldRule): FieldRule => rest;

function stripLoopId({ id: _id, ...loop }: EditorLoopRule): LoopRule {
  return {
    ...loop,
    item: loop.item ? stripFieldId(loop.item) : undefined,
    fields: loop.fields.map(stripFieldId),
    loops: loop.loops.map(stripLoopId),
  };
}

/** Drops the editor's ids, leaving exactly what the mapper reads. */
export function toWire(rules: EditorRules): MappingRules {
  return {
    version: rules.version,
    sourceFormat: rules.sourceFormat,
    targetFormat: rules.targetFormat,
    fields: rules.fields.map(stripFieldId),
    loops: rules.loops.map(stripLoopId),
    root: rules.root ? stripLoopId(rules.root) : undefined,
  };
}

/** Adds the editor's ids to rules that came off the wire. */
export function fromWire(rules: MappingRules): EditorRules {
  return {
    version: rules.version ?? RULES_VERSION,
    sourceFormat: (rules.sourceFormat ?? "json") as DocumentFormatId,
    targetFormat: (rules.targetFormat ?? "json") as DocumentFormatId,
    fields: withFieldIds(rules.fields),
    loops: withLoopIds(rules.loops),
    root: rules.root ? loopWithIds(rules.root) : undefined,
  };
}

export interface LoadedMapping {
  rules: EditorRules;
  sourceSample: string;
  targetSample: string;
  /** Set when stored rules could not be read, so the editor can say so rather than silently starting blank. */
  error?: string;
}

/**
 * Reads a mapping out of a subscription's mapper properties.
 *
 * Unreadable rules are reported rather than swallowed. Starting the editor blank
 * and letting the user save over a mapping that was merely unparseable would
 * destroy it, which is worse than refusing to open.
 */
export function loadMapping(properties: Record<string, string> | undefined): LoadedMapping {
  const raw = properties?.[MAPPING_RULES_KEY];
  const sourceSample = properties?.[SOURCE_SAMPLE_KEY] ?? "";
  const targetSample = properties?.[TARGET_SAMPLE_KEY] ?? "";

  if (!raw?.trim()) return { rules: emptyRules(), sourceSample, targetSample };

  let parsed: MappingRules;
  try {
    parsed = JSON.parse(raw) as MappingRules;
  } catch (e) {
    return {
      rules: emptyRules(),
      sourceSample,
      targetSample,
      error: `The saved mapping rules could not be read: ${(e as Error).message}`,
    };
  }

  if (typeof parsed?.version === "number" && parsed.version > RULES_VERSION) {
    return {
      rules: emptyRules(),
      sourceSample,
      targetSample,
      error:
        `These mapping rules are version ${parsed.version}, but this version of Bitween ` +
        `understands up to version ${RULES_VERSION}. Opening them here would lose whatever ` +
        `the newer version added.`,
    };
  }

  return { rules: fromWire(parsed), sourceSample, targetSample };
}

/** The mapper properties to save for a mapping. */
export function saveMapping(
  rules: EditorRules,
  sourceSample: string,
  targetSample: string,
): Record<string, string> {
  return {
    [MAPPING_RULES_KEY]: JSON.stringify(toWire(rules)),
    [SOURCE_SAMPLE_KEY]: sourceSample,
    [TARGET_SAMPLE_KEY]: targetSample,
  };
}
