// ─── The shape of a sample document, as the mapper sees it ───────────────────
//
// Deliberately not the old editor's buildTree. That one makes a list behave as its
// own first element, so `order.line.sku` shows up as a leaf you can map — which
// silently meant "the first line's sku" and had no way to say anything else. The
// new mapper does not do that: a path stops at a list, and a list is something a
// loop walks. The tree has to show the same thing, or the editor would offer paths
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
   * because that is what a loop's field rules are written against.
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
    // path a rule inside the loop uses.
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
 * A path inside a list is reachable only from a loop over that list, so offering it
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
    if (node.kind === "list") return; // its contents belong to a loop
    node.children.forEach(walk);
  };
  walk(root);
  return out;
}

/** Every path that holds a list, which is what a loop can walk. */
export function listPaths(root: DocumentNode | null): string[] {
  if (!root) return [];
  const out: string[] = [];
  const walk = (node: DocumentNode) => {
    if (node.kind === "list") {
      // The root itself is a list for a root-array document; a loop says so with an
      // empty `over`, so it is offered as "" rather than skipped.
      out.push(node.path);
      return;
    }
    if (node.kind === "object") node.children.forEach(walk);
  };
  walk(root);
  return out;
}

/** The item shape of the list at a path, for a loop's field rules. */
export function itemShapeAt(root: DocumentNode | null, path: string): DocumentNode[] {
  if (!root) return [];
  const found = findNode(root, path);
  return found?.kind === "list" ? found.children : [];
}

export function findNode(root: DocumentNode, path: string): DocumentNode | undefined {
  if (root.path === path) return root;
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
