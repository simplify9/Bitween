import { CornerDownRight, Trash2 } from "lucide-react";
import type { OutputEntryNode } from "../../lib/nativeMapper/outputTree";
import { useRules, useRulesDispatch } from "../../lib/nativeMapper/RulesEditorContext";
import { SOURCE_KINDS, freshSource } from "../../lib/nativeMapper/types";
import { SegmentedControl } from "../ui/SegmentedControl";
import { ValueCell, type SourcePaths } from "./ValueCell";

/**
 * One entry written into a list, rather than one produced by walking a source list.
 *
 * A header line a partner expects, or one slot of a list whose entries each come
 * from somewhere different. Its rules read whatever the list reads, because there
 * is no entry of its own to read — which is what lets a constant entry still pull a
 * value out of the document, the partner or a values set.
 */
export function EntryRow({
  node,
  /** The paths this entry may read — the list's own scope, not an entry of it. */
  paths,
}: {
  node: OutputEntryNode;
  paths: SourcePaths;
}) {
  const { entry, position } = node;
  const dispatch = useRulesDispatch();
  const { ruleErrors } = useRules();

  const error = ruleErrors[node.errorKey];
  const isValue = entry.item !== undefined;

  return (
    <div
      // Named, so this entry's own controls can be told apart from the list's and
      // from the other entries' — the position is the only thing distinguishing them.
      role="group"
      aria-label={`Entry ${position}`}
      className={`rounded border ${
        error ? "border-danger-300 bg-danger-50" : "border-warn-200/70 bg-warn-100/20"
      }`}
    >
      <div className="flex items-center gap-1.5 px-1.5 py-0.5">
        <CornerDownRight size={11} className="flex-shrink-0 text-warn-700" aria-hidden />
        <span className="flex-shrink-0 text-[11px] font-medium text-warn-700">
          entry {position}
        </span>

        {isValue && entry.item && (
          <>
            <span aria-hidden className="flex-shrink-0 text-ink-300">
              ←
            </span>
            <SegmentedControl
              size="sm"
              label={`Where entry ${position} comes from`}
              options={SOURCE_KINDS}
              value={entry.item.from.kind === "rootPath" ? "path" : entry.item.from.kind}
              onChange={(kind) =>
                dispatch({
                  type: "UPDATE_FIXED_ENTRY",
                  id: entry.id,
                  changes: { item: { ...entry.item!, from: freshSource(kind) } },
                })
              }
            />
            <ValueCell
              source={entry.item.from}
              paths={paths}
              valueType={entry.item.type}
              onChange={(from) =>
                dispatch({
                  type: "UPDATE_FIXED_ENTRY",
                  id: entry.id,
                  changes: { item: { ...entry.item!, from } },
                })
              }
            />
          </>
        )}

        <button
          type="button"
          onClick={() => dispatch({ type: "REMOVE_FIXED_ENTRY", id: entry.id })}
          aria-label={`Remove entry ${position}`}
          title="Remove this entry"
          className="ml-auto flex-shrink-0 rounded p-0.5 text-ink-300 hover:bg-danger-50 hover:text-danger-700"
        >
          <Trash2 size={12} />
        </button>
      </div>
    </div>
  );
}
