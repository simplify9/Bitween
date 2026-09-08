import { Plus, Repeat, Trash2 } from "lucide-react";
import { useRules, useRulesDispatch } from "../../lib/nativeMapper/RulesEditorContext";
import { targetPathOf } from "../../lib/nativeMapper/rulesReducer";
import {
  FILTER_OPERATORS,
  emptyFieldRule,
  type EditorLoopRule,
  type FilterOperatorName,
} from "../../lib/nativeMapper/types";
import { Button } from "../ui/basics";
import { Checkbox, Select, TextInput } from "../ui/forms";
import { FieldRuleRow } from "./FieldRuleRow";
import { ValueSourceFields } from "./ValueSourceFields";

/**
 * A loop: one output entry per entry in a list on the source document.
 *
 * Nested loops live inside their parent rather than in a flat list joined by ids,
 * which is how the old model did it — that needed three recursive helpers to work
 * out a rule's real path, and deleting a parent left its children unreachable.
 */
export function LoopRuleCard({
  loop,
  listPathOptions,
  /** The path prefix errors inside this loop are reported under. */
  errorPrefix,
  depth,
  isRoot = false,
}: {
  loop: EditorLoopRule;
  listPathOptions: string[];
  errorPrefix: string;
  depth: number;
  isRoot?: boolean;
}) {
  const dispatch = useRulesDispatch();
  const { ruleErrors } = useRules();

  const target = isRoot ? "" : targetPathOf(loop.target);
  const ownErrorKey = errorPrefix ? `${errorPrefix}[].${target}` : target;
  const loopError = ruleErrors[ownErrorKey];
  const childPrefix = ownErrorKey;

  const update = (changes: Partial<Omit<EditorLoopRule, "id" | "fields" | "loops">>) =>
    dispatch({ type: "UPDATE_LOOP", id: loop.id, changes });

  const producesValues = loop.item !== undefined;

  return (
    <div
      // A group with a name, so it is possible to tell which list a rule row belongs
      // to — the nesting is visual otherwise, which says nothing to a screen reader.
      role="group"
      aria-label={isRoot ? "Root list rules" : `List ${target || "(unnamed)"} rules`}
      className={`rounded-lg border px-2.5 py-2 ${
        loopError ? "border-danger-300 bg-danger-50" : "border-warn-300 bg-warn-100/40"
      }`}
    >
      <div className="mb-2 flex flex-wrap items-center gap-2">
        <Repeat size={13} className="flex-shrink-0 text-warn-700" aria-hidden />
        <span className="text-[11px] font-semibold tracking-wide text-warn-700 uppercase">
          {isRoot ? "Root list" : "List"}
        </span>

        {!isRoot && (
          <TextInput
            className="h-8 w-40 font-mono text-xs"
            aria-label="Output list name"
            placeholder="lines"
            title="The name of the list in the output. Use dots to nest."
            value={loop.target.join(".")}
            onChange={(e) =>
              update({ target: e.target.value === "" ? [] : e.target.value.split(".") })
            }
          />
        )}

        <span className="text-[11px] text-ink-600">for each entry in</span>

        <Select
          className="h-8 w-44 font-mono text-xs"
          aria-label="Source list"
          title="Which list on the source document to walk"
          value={loop.over}
          onChange={(e) => update({ over: e.target.value })}
          options={[
            ...(listPathOptions.includes(loop.over)
              ? []
              : [{ value: loop.over, label: loop.over || "Choose a list…" }]),
            ...listPathOptions.map((p) => ({ value: p, label: p === "" ? "(the document)" : p })),
          ]}
        />

        <button
          type="button"
          onClick={() => dispatch({ type: "REMOVE_LOOP", id: loop.id })}
          aria-label={isRoot ? "Remove the root list" : `Remove the list ${target}`}
          title="Remove this list"
          className="ml-auto rounded p-1 text-ink-400 hover:bg-danger-50 hover:text-danger-700"
        >
          <Trash2 size={13} />
        </button>
      </div>

      {/* ── Filter ─────────────────────────────────────────────────────────── */}
      <div className="mb-2 flex flex-wrap items-center gap-1.5">
        <Checkbox
          label="Only some entries"
          checked={loop.where !== undefined}
          onChange={(e) =>
            update({
              where: e.target.checked ? { field: "", operator: "equal", value: "" } : undefined,
            })
          }
        />
        {loop.where && (
          <>
            <TextInput
              className="h-8 w-28 font-mono text-xs"
              aria-label="Filter field"
              placeholder="qty"
              title="A field of the entry, not of the whole document"
              value={loop.where.field}
              onChange={(e) => update({ where: { ...loop.where!, field: e.target.value } })}
            />
            <Select
              className="h-8 w-16 text-xs"
              aria-label="Filter comparison"
              value={loop.where.operator}
              onChange={(e) =>
                update({
                  where: { ...loop.where!, operator: e.target.value as FilterOperatorName },
                })
              }
              options={FILTER_OPERATORS.map((o) => ({ value: o.value, label: o.label }))}
            />
            <TextInput
              className="h-8 w-24 text-xs"
              aria-label="Filter value"
              placeholder="0"
              value={String(loop.where.value ?? "")}
              onChange={(e) => update({ where: { ...loop.where!, value: e.target.value } })}
            />
          </>
        )}
      </div>

      {/* ── What each entry produces ───────────────────────────────────────── */}
      <div className="mb-2">
        <Checkbox
          label="A list of plain values, not records"
          checked={producesValues}
          onChange={(e) => update({ item: e.target.checked ? emptyFieldRule() : undefined })}
        />
        <p className="mt-0.5 text-[11px] text-ink-500">
          {producesValues
            ? 'Each entry becomes a single value, e.g. ["A1", "B7"].'
            : 'Each entry becomes an object, e.g. [{ "sku": "A1" }].'}
        </p>
      </div>

      {producesValues && loop.item ? (
        <div className="rounded-lg border border-ink-200 bg-white px-2.5 py-2">
          <ValueSourceFields
            rule={loop.item}
            onChange={(changes) =>
              update({ item: { ...loop.item!, ...changes } })
            }
          />
        </div>
      ) : (
        <div className="space-y-1.5">
          {loop.fields.map((field) => (
            <FieldRuleRow
              key={field.id}
              rule={field}
              errorKey={childPrefix ? `${childPrefix}[].${targetPathOf(field.target)}` : targetPathOf(field.target)}
              onRemove={() => dispatch({ type: "REMOVE_FIELD", id: field.id })}
            />
          ))}

          {loop.loops.map((nested) => (
            <LoopRuleCard
              key={nested.id}
              loop={nested}
              listPathOptions={listPathOptions}
              errorPrefix={childPrefix}
              depth={depth + 1}
            />
          ))}

          <div className="flex flex-wrap gap-1.5 pt-0.5">
            <Button
              size="sm"
              onClick={() => dispatch({ type: "ADD_FIELD", loopId: loop.id })}
            >
              <Plus size={12} /> Field
            </Button>
            {/* Three levels is as deep as any document we have needed to map. Beyond
                that the card nesting stops being readable, so it is not offered. */}
            {depth < 2 && (
              <Button
                size="sm"
                onClick={() => dispatch({ type: "ADD_LOOP", parentLoopId: loop.id })}
              >
                <Plus size={12} /> List inside this one
              </Button>
            )}
          </div>
        </div>
      )}

      {loopError && (
        <p role="alert" className="mt-1.5 font-mono text-[11px] text-danger-700">
          {loopError}
        </p>
      )}
    </div>
  );
}
