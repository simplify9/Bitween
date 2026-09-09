import {
  TRANSFORMS,
  VALUE_TYPES,
  type EditorFieldRule,
  type TransformRule,
} from "../../lib/nativeMapper/types";
import { RowInput, RowSelect } from "./rowControls";
import { LookupFields } from "./LookupFields";


/**
 * Everything about one rule that does not fit on its line.
 *
 * Laid out in the order the mapper applies it — the value, then the transform,
 * then the table, then the type — so the panel reads as the pipeline it is.
 */
/**
 * A numeric argument as a number, unless that would change what was typed.
 *
 * `Number("1.")` is 1, so storing the number and rendering it back deleted the point
 * as it was typed and made "1.16" impossible to enter. Text is kept whenever the
 * number would not round-trip to the same characters — the server parses a numeric
 * string for these arguments either way.
 */
function numericOrText(kind: "text" | "number", text: string): string | number {
  if (kind !== "number") return text;
  const asNumber = Number(text);
  return !isNaN(asNumber) && String(asNumber) === text.trim() ? asNumber : text;
}

export function RuleDetail({
  rule,
  onChange,
}: {
  rule: EditorFieldRule;
  onChange: (changes: Partial<Omit<EditorFieldRule, "id">>) => void;
}) {
  return (
    <div className="mt-1 ml-4 space-y-2 border-l-2 border-ink-100 pl-2.5">
      <Step n={2} label="Change it">
        <TransformFields
          transform={rule.transform}
          onChange={(transform) => onChange({ transform })}
        />
      </Step>

      <Step n={3} label="Substitute it">
        <LookupFields lookup={rule.lookup} onChange={(lookup) => onChange({ lookup })} />
      </Step>

      <Step n={4} label="Write it as">
        <RowSelect
          className="w-32"
          aria-label="Value type"
          title="A value that cannot be converted fails the exchange rather than arriving empty."
          value={rule.type ?? ""}
          onChange={(e) =>
            onChange({ type: e.target.value === "" ? undefined : (e.target.value as never) })
          }
          options={VALUE_TYPES}
        />
      </Step>
    </div>
  );
}

/** The step's place in the pipeline, numbered from the value on the row itself. */
function Step({ n, label, children }: { n: number; label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="mb-1 text-[10px] font-semibold tracking-wide text-ink-400 uppercase">
        {n}. {label}
      </p>
      {children}
    </div>
  );
}

/**
 * The transform dropdown and whatever arguments the chosen function takes.
 *
 * A closed list rather than a free-text expression, so there is no syntax to learn
 * and nothing to parse on the server. The names mirror the functions the mapper
 * implements, and a test on each side keeps the two lists in step.
 *
 * One transform per rule: they do not chain.
 */
export function TransformFields({
  transform,
  onChange,
}: {
  transform: TransformRule | undefined;
  onChange: (transform: TransformRule | undefined) => void;
}) {
  const chosen = TRANSFORMS.find((t) => t.fn === transform?.fn);

  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <RowSelect
        className="w-44"
        aria-label="Transform"
        value={transform?.fn ?? ""}
        onChange={(e) => onChange(e.target.value === "" ? undefined : { fn: e.target.value })}
        options={[
          { value: "", label: "No transform" },
          ...TRANSFORMS.map((t) => ({ value: t.fn, label: t.label })),
        ]}
      />

      {chosen?.args.map((arg) => (
        <RowInput
          key={arg.name}
          className="w-28"
          aria-label={`${chosen.label} — ${arg.label}`}
          placeholder={arg.label}
          inputMode={arg.kind === "number" ? "decimal" : undefined}
          value={String(transform?.[arg.name] ?? "")}
          onChange={(e) => {
            const next: TransformRule = { ...(transform ?? { fn: chosen.fn }) };
            if (e.target.value === "") delete next[arg.name];
            else next[arg.name] = numericOrText(arg.kind, e.target.value);
            onChange(next);
          }}
        />
      ))}
    </div>
  );
}
