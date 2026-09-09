// ─── The shape of a sample document, as the mapper sees it ───────────────────
//
// Deliberately not the old editor's buildTree. That one makes a list behave as its
// own first element, so `order.line.sku` shows up as a leaf you can map — which
// silently meant "the first line's sku" and had no way to say anything else. The
// new mapper does not do that: a path stops at a list, and a list is something a
// list walks. The tree has to show the same thing, or the editor would offer paths
// that resolve to null.

export type DocumentNodeKind = "value" | "object" | "list";

export interface DocumentNode {
  /** The key, as it appears in the document. */
  key: string;
  /** Dot-separated path from the root. Empty for the root itself. */
  path: string;
  kind: DocumentNodeKind;
  /** A sample of the value, for `value` nodes. */
  sample?: unknown;
  /** How many entries the list had in the sample. */
  count?: number;
  /**
   * For a list, the shape of its items — the paths inside are relative to the item,
   * because that is what a list's field rules are written against.
   */
  children: DocumentNode[];
}

export interface ParsedSample {
  /** Null when the text could not be read. */
  root: DocumentNode | null;
  error: string | null;
}

/**
 * Reads a sample document into a tree.
 *
 * Only JSON for now. When another format arrives it parses here — and until then a
 * mapping whose source format is not JSON simply has no tree to show, which is
 * honest rather than showing a JSON one.
 */
export function parseSample(text: string, format: string): ParsedSample {
  if (!text.trim()) return { root: null, error: null };

  if (format !== "json") {
    return { root: null, error: `No preview of the document shape for ${format} yet.` };
  }

  try {
    return { root: toNode("", "", JSON.parse(text)), error: null };
  } catch (e) {
    return { root: null, error: `The sample is not valid JSON: ${(e as Error).message}` };
  }
}

function toNode(key: string, path: string, value: unknown): DocumentNode {
  if (Array.isArray(value)) {
    // The first entry stands for the shape of every entry, and its children are
    // named relative to the item — `sku`, not `line[0].sku` — because that is the
    // path a rule inside the list uses.
    const first = value.length > 0 ? value[0] : undefined;
    return {
      key,
      path,
      kind: "list",
      count: value.length,
      children:
        first !== null && typeof first === "object" && !Array.isArray(first)
          ? Object.entries(first as Record<string, unknown>).map(([k, v]) => toNode(k, k, v))
          : [],
    };
  }

  if (value !== null && typeof value === "object") {
    return {
      key,
      path,
      kind: "object",
      children: Object.entries(value as Record<string, unknown>).map(([k, v]) =>
        toNode(k, path ? `${path}.${k}` : k, v),
      ),
    };
  }

  return { key, path, kind: "value", sample: value, children: [] };
}

/**
 * Every path a field rule can read, which is every `value` node not inside a list.
 *
 * A path inside a list is reachable only from a list that walks it, so offering it
 * at the top level would offer a mapping that resolves to null.
 */
export function readablePaths(root: DocumentNode | null): string[] {
  if (!root) return [];
  const out: string[] = [];
  const walk = (node: DocumentNode) => {
    if (node.kind === "value") {
      if (node.path) out.push(node.path);
      return;
    }
    if (node.kind === "list") return; // its contents belong to a list
    node.children.forEach(walk);
  };
  walk(root);
  return out;
}

/** Every path that holds a list, which is what a list can walk. */
export function listPaths(root: DocumentNode | null): string[] {
  if (!root) return [];
  const out: string[] = [];
  const walk = (node: DocumentNode) => {
    if (node.kind === "list") {
      // The root itself is a list for a root-array document; a list says so with an
      // empty `over`, so it is offered as "" rather than skipped.
      out.push(node.path);
      return;
    }
    if (node.kind === "object") node.children.forEach(walk);
  };
  walk(root);
  return out;
}

/** The item shape of the list at a path, for a list's field rules. */
export function itemShapeAt(root: DocumentNode | null, path: string): DocumentNode[] {
  if (!root) return [];
  const found = findNode(root, path);
  return found?.kind === "list" ? found.children : [];
}

/**
 * What the rules inside a list read their paths against: one entry of the list at
 * `over`, wrapped so it can be walked like a document of its own.
 *
 * This is the scope the mapper itself uses — `MapInto` hands a list's nested lists the
 * current item, not the document — so a list nested in another names its list
 * relative to the entry around it (`tags`), never from the root (`order.line.tags`).
 */
export function itemScopeOf(scope: DocumentNode | null, over: string): DocumentNode {
  return { key: "", path: "", kind: "object", children: itemShapeAt(scope, over) };
}

/**
 * The node at a path, within one namespace.
 *
 * A list's children are named relative to one entry — `sku`, not `order.line.sku` — so
 * they live in a different namespace to everything above them. Searching through a list
 * would let `findNode(root, "tag")` answer with a `tag` nested inside some list when the
 * caller meant a `tag` at the top, which is how `itemShapeAt` could scope a list's rules
 * against the wrong entry shape.
 */
export function findNode(root: DocumentNode, path: string): DocumentNode | undefined {
  if (root.path === path) return root;
  if (root.kind === "list") return undefined;
  for (const child of root.children) {
    const found = findNode(child, path);
    if (found) return found;
  }
  return undefined;
}

/** A value shown next to a source field, short enough to sit on one line. */
export function describeSample(value: unknown): string {
  if (value === null) return "null";
  if (typeof value === "string") return value.length > 24 ? `"${value.slice(0, 24)}…"` : `"${value}"`;
  if (typeof value === "boolean" || typeof value === "number") return String(value);
  return "";
}

export interface Coverage {
  mapped: number;
  total: number;
}

/**
 * How much of what is under each object and list some rule already reads.
 *
 * Built once for the whole tree rather than asked per row: every row subscribes to the
 * hovered path, so each hover re-rendered all of them, and a per-row walk of its own
 * subtree made that quadratic on a document with any depth to it.
 *
 * Counted over the paths as they are named here, which for a list's contents is
 * relative to one entry — the same name a rule inside that list uses, so the two
 * halves agree.
 */
export function coverageByPath(
  root: DocumentNode | null,
  assignedPaths: Set<string>,
): Map<string, Coverage> {
  const out = new Map<string, Coverage>();
  if (!root) return out;

  const walk = (node: DocumentNode): Coverage => {
    if (node.kind === "value")
      return { mapped: assignedPaths.has(node.path) ? 1 : 0, total: 1 };

    let mapped = 0;
    let total = 0;
    for (const child of node.children) {
      const under = walk(child);
      mapped += under.mapped;
      total += under.total;
    }
    out.set(node.path, { mapped, total });
    return { mapped, total };
  };

  walk(root);
  return out;
}
