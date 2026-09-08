import { Plus, Search } from "lucide-react";
import { useRules, useRulesDispatch } from "../../lib/nativeMapper/RulesEditorContext";
import { everyFieldRule, isAssigned, targetPathOf } from "../../lib/nativeMapper/rulesReducer";
import { Button } from "../ui/basics";
import { Checkbox, TextInput } from "../ui/forms";
import { FieldRuleRow } from "./FieldRuleRow";
import { LoopRuleCard } from "./LoopRuleCard";

/** The rules that make up the output document. */
export function OutputPanel({ listPathOptions }: { listPathOptions: string[] }) {
  const { rules, searchTarget } = useRules();
  const dispatch = useRulesDispatch();

  const all = everyFieldRule(rules);
  const assignedCount = all.filter((f) => isAssigned(f.rule)).length;
  const search = searchTarget.trim().toLowerCase();

  const matches = (target: string[]) =>
    search.length === 0 || targetPathOf(target).toLowerCase().includes(search);

  return (
    <div className="flex flex-1 flex-col overflow-hidden">
      <div className="flex-shrink-0 border-b border-ink-100 bg-ink-50 px-3 py-2">
        <div className="mb-1.5 flex items-center gap-2">
          <span className="text-xs font-semibold tracking-wide text-ink-500 uppercase">Output</span>
          <span
            className="rounded bg-ink-100 px-1.5 py-0.5 font-mono text-[10px] text-ink-600"
            title="The format of the document sent onward"
          >
            {rules.targetFormat}
          </span>
          <span className="text-xs text-ink-400" title="How many rules have a value assigned">
            {all.length} {all.length === 1 ? "rule" : "rules"} · {assignedCount} assigned
          </span>
        </div>
        <div className="relative">
          <Search className="absolute top-1.5 left-2 text-ink-400" size={11} />
          <TextInput
            className="h-7 pl-6 font-mono text-xs"
            placeholder="Search output fields…"
            value={searchTarget}
            onChange={(e) => dispatch({ type: "SET_SEARCH_TARGET", text: e.target.value })}
          />
        </div>
      </div>

      <div className="flex-shrink-0 border-b border-ink-100 px-3 py-2">
        <Checkbox
          label="The whole output is a list"
          description="For a partner that expects a bare array rather than an object with a list inside it."
          checked={rules.root !== undefined}
          onChange={(e) => dispatch({ type: "SET_ROOT_LOOP", enabled: e.target.checked })}
        />
      </div>

      <div className="flex-1 space-y-1.5 overflow-y-auto px-3 py-2">
        {rules.root ? (
          <LoopRuleCard
            loop={rules.root}
            listPathOptions={listPathOptions}
            errorPrefix=""
            depth={0}
            isRoot
          />
        ) : (
          <>
            {rules.fields.filter((f) => matches(f.target)).map((field) => (
              <FieldRuleRow
                key={field.id}
                rule={field}
                errorKey={targetPathOf(field.target)}
                onRemove={() => dispatch({ type: "REMOVE_FIELD", id: field.id })}
              />
            ))}

            {rules.loops.filter((l) => matches(l.target)).map((loop) => (
              <LoopRuleCard
                key={loop.id}
                loop={loop}
                listPathOptions={listPathOptions}
                errorPrefix=""
                depth={0}
              />
            ))}

            {rules.fields.length === 0 && rules.loops.length === 0 && (
              <p className="py-6 text-center text-xs text-ink-400">
                No rules yet. Add a field, then pick where its value comes from.
              </p>
            )}

            <div className="flex flex-wrap gap-1.5 pt-1">
              <Button size="sm" onClick={() => dispatch({ type: "ADD_FIELD", loopId: null })}>
                <Plus size={12} /> Field
              </Button>
              <Button size="sm" onClick={() => dispatch({ type: "ADD_LOOP", parentLoopId: null })}>
                <Plus size={12} /> List
              </Button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
