import { Trash2 } from "lucide-react";
import { useRules, useRulesDispatch } from "../../lib/nativeMapper/RulesEditorContext";
import { targetPathOf } from "../../lib/nativeMapper/rulesReducer";
import type { EditorFieldRule } from "../../lib/nativeMapper/types";
import { TextInput } from "../ui/forms";
import { TransformFields, ValueSourceFields } from "./ValueSourceFields";

/**
 * One output field.
 *
 * The target is typed with dots for nesting and split into segments on the way into
 * the rule, so `customer.name` nests — but a segment that genuinely contains a dot
 * is still reachable, because the rule stores segments rather than the string.
 */
export function FieldRuleRow({
  rule,
  /** The path errors are reported under, which includes the loop for a rule inside one. */
  errorKey,
  onRemove,
}: {
  rule: EditorFieldRule;
  errorKey: string;
  onRemove: () => void;
}) {
  const dispatch = useRulesDispatch();
  const { selectedId, ruleErrors } = useRules();

  const selected = selectedId === rule.id;
  const error = ruleErrors[errorKey];

  const update = (changes: Partial<Omit<EditorFieldRule, "id">>) =>
    dispatch({ type: "UPDATE_FIELD", id: rule.id, changes });

  return (
    <div
      onFocus={() => dispatch({ type: "SELECT", id: rule.id })}
      className={`rounded-lg border px-2.5 py-2 ${
        error
          ? "border-danger-300 bg-danger-50"
          : selected
            ? "border-crimson-300 bg-crimson-50"
            : "border-ink-200 bg-white"
      }`}
    >
      <div className="mb-1.5 flex items-center gap-2">
        <TextInput
          className="h-8 flex-1 font-mono text-xs"
          aria-label="Output field name"
          placeholder="customerName"
          title="Use dots to nest, e.g. customer.name"
          value={rule.target.join(".")}
          onChange={(e) =>
            update({
              target: e.target.value === "" ? [] : e.target.value.split("."),
            })
          }
        />
        <button
          type="button"
          onClick={onRemove}
          aria-label={`Remove the rule for ${targetPathOf(rule.target)}`}
          title="Remove this rule"
          className="rounded p-1 text-ink-400 hover:bg-danger-50 hover:text-danger-700"
        >
          <Trash2 size={13} />
        </button>
      </div>

      <ValueSourceFields rule={rule} onChange={update} />

      <div className="mt-1.5">
        <TransformFields transform={rule.transform} onChange={(t) => update({ transform: t })} />
      </div>

      {error && (
        <p role="alert" className="mt-1.5 font-mono text-[11px] text-danger-700">
          {error}
        </p>
      )}
    </div>
  );
}
