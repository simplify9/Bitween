import { Search } from "lucide-react";
import { describeSample, type DocumentNode } from "../../lib/nativeMapper/documentTree";
import { useRules, useRulesDispatch } from "../../lib/nativeMapper/RulesEditorContext";
import { TextInput } from "../ui/forms";

/**
 * The shape of the source document, as a tree of paths a rule can read.
 *
 * A list is shown but not offered as a value — its contents are reachable only from
 * a loop over it, and offering them here would offer a mapping that resolves to
 * null. The old editor did offer them, by making a list behave as its own first
 * element.
 */
export function SourcePanel({
  root,
  parseError,
  assignedPaths,
  onPick,
}: {
  root: DocumentNode | null;
  parseError: string | null;
  assignedPaths: Set<string>;
  /** Called when a path is clicked, to fill whichever rule is selected. */
  onPick: (path: string) => void;
}) {
  const { searchSource, sourceSample, rules } = useRules();
  const dispatch = useRulesDispatch();

  return (
    <div className="flex w-[30%] min-w-[260px] max-w-[400px] flex-col overflow-hidden border-r border-ink-200">
      <div className="flex-shrink-0 border-b border-ink-100 bg-ink-50 px-3 py-2">
        <div className="mb-1.5 flex items-center gap-2">
          <span className="text-xs font-semibold tracking-wide text-ink-500 uppercase">Source</span>
          <span
            className="rounded bg-ink-100 px-1.5 py-0.5 font-mono text-[10px] text-ink-600"
            title="The format of the document arriving from the partner"
          >
            {rules.sourceFormat}
          </span>
        </div>
        <div className="relative">
          <Search className="absolute top-1.5 left-2 text-ink-400" size={11} />
          <TextInput
            className="h-7 pl-6 font-mono text-xs"
            placeholder="Search fields…"
            value={searchSource}
            onChange={(e) => dispatch({ type: "SET_SEARCH_SOURCE", text: e.target.value })}
          />
        </div>
      </div>

      <div className="flex-shrink-0 border-b border-ink-100 px-3 py-2">
        <textarea
          className="min-h-[72px] w-full resize-y rounded-lg border border-ink-200 bg-ink-50 px-2 py-1.5 font-mono text-[11px] focus:border-crimson-400 focus:outline-none"
          placeholder='Paste a sample document, e.g. { "order": { "customer": "Ali" } }'
          value={sourceSample}
          onChange={(e) => dispatch({ type: "SET_SOURCE_SAMPLE", text: e.target.value })}
          aria-label="Sample source document"
        />
        {parseError && <p className="mt-1 text-[11px] text-danger-700">{parseError}</p>}
        <p className="mt-1 text-[11px] text-ink-500">
          Used only here, to show the fields and the preview. The mapping never reads it.
        </p>
      </div>

      <div className="flex-1 overflow-y-auto py-1">
        {!root ? (
          <p className="px-3 py-4 text-xs text-ink-400">
            Paste a sample document to see its fields.
          </p>
        ) : (
          <NodeRows
            nodes={root.kind === "object" ? root.children : [root]}
            depth={0}
            search={searchSource.trim().toLowerCase()}
            assignedPaths={assignedPaths}
            onPick={onPick}
          />
        )}
      </div>
    </div>
  );
}

function NodeRows({
  nodes,
  depth,
  search,
  assignedPaths,
  onPick,
}: {
  nodes: DocumentNode[];
  depth: number;
  search: string;
  assignedPaths: Set<string>;
  onPick: (path: string) => void;
}) {
  return (
    <>
      {nodes.map((node) => (
        <NodeRow
          key={node.path || node.key || "root"}
          node={node}
          depth={depth}
          search={search}
          assignedPaths={assignedPaths}
          onPick={onPick}
        />
      ))}
    </>
  );
}

function NodeRow({
  node,
  depth,
  search,
  assignedPaths,
  onPick,
}: {
  node: DocumentNode;
  depth: number;
  search: string;
  assignedPaths: Set<string>;
  onPick: (path: string) => void;
}) {
  const dispatch = useRulesDispatch();
  const { hoveredPath } = useRules();

  const matches = search.length > 0 && node.path.toLowerCase().includes(search);
  const dim = search.length > 0 && !matches && !hasDescendantMatch(node, search);
  const pad = { paddingLeft: `${depth * 12 + 12}px` };

  if (node.kind === "value") {
    const assigned = assignedPaths.has(node.path);
    return (
      <button
        type="button"
        style={pad}
        onClick={() => onPick(node.path)}
        onMouseEnter={() => dispatch({ type: "HOVER_PATH", path: node.path })}
        onMouseLeave={() => dispatch({ type: "HOVER_PATH", path: null })}
        title={`${node.path} — click to use it in the selected rule`}
        className={`flex w-full items-center gap-2 py-0.5 pr-2 text-left font-mono text-[11px] hover:bg-ink-50 ${
          dim ? "opacity-40" : ""
        } ${hoveredPath === node.path ? "bg-crimson-50" : ""}`}
      >
        <span
          aria-hidden
          className={`size-1.5 flex-shrink-0 rounded-full ${assigned ? "bg-ok-600" : "bg-ink-300"}`}
        />
        <span className={`truncate ${matches ? "font-semibold text-crimson-700" : "text-ink-800"}`}>
          {node.key}
        </span>
        <span className="ml-auto flex-shrink-0 truncate text-ink-400">
          {describeSample(node.sample)}
        </span>
      </button>
    );
  }

  return (
    <div className={dim ? "opacity-40" : ""}>
      <div
        style={pad}
        className="flex items-center gap-2 py-0.5 pr-2 font-mono text-[11px] text-ink-600"
      >
        <span className="truncate font-semibold">{node.key || "(root)"}</span>
        {node.kind === "list" ? (
          <span
            className="rounded bg-warn-100 px-1 text-[9px] font-semibold tracking-wide text-warn-700 uppercase"
            title="A list. Add a loop on the output side to walk it — its fields are not readable on their own."
          >
            list · {node.count}
          </span>
        ) : (
          <span className="text-ink-400" title="An object">
            {"{}"}
          </span>
        )}
      </div>
      {node.kind === "object" && (
        <NodeRows
          nodes={node.children}
          depth={depth + 1}
          search={search}
          assignedPaths={assignedPaths}
          onPick={onPick}
        />
      )}
    </div>
  );
}

function hasDescendantMatch(node: DocumentNode, search: string): boolean {
  return node.children.some(
    (child) => child.path.toLowerCase().includes(search) || hasDescendantMatch(child, search),
  );
}
