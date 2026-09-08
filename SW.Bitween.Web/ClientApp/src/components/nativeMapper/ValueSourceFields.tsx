import { useQuery } from "@tanstack/react-query";
import { api } from "../../api";
import { keys } from "../../api/queryKeys";
import {
  TRANSFORMS,
  type EditorFieldRule,
  type TransformRule,
  type ValueSource,
  type ValueSourceKind,
  type ValueTypeName,
} from "../../lib/nativeMapper/types";
import { Select, TextInput } from "../ui/forms";

const KINDS: { value: ValueSourceKind; label: string; title: string }[] = [
  { value: "path", label: "From the document", title: "Read a field out of the incoming document" },
  { value: "fixed", label: "A fixed value", title: "The same value every time" },
  { value: "partner", label: "Partner property", title: "A property of the exchange's partner" },
  { value: "global", label: "Global value", title: "A key from one of the global values sets" },
];

const TYPES: { value: string; label: string }[] = [
  { value: "", label: "As it comes" },
  { value: "string", label: "Text" },
  { value: "number", label: "Number" },
  { value: "boolean", label: "Yes / no" },
];

/**
 * The controls for where one rule's value comes from.
 *
 * Changing the kind replaces the whole source rather than adding to it, so two
 * sources cannot both be set — which is what the old editor's mode switching kept
 * getting wrong, since it had six optional fields and had to remember to clear five.
 */
export function ValueSourceFields({
  rule,
  onChange,
}: {
  rule: EditorFieldRule;
  onChange: (changes: Partial<Omit<EditorFieldRule, "id">>) => void;
}) {
  const setKind = (kind: ValueSourceKind) => {
    const fresh: Record<ValueSourceKind, ValueSource> = {
      path: { kind: "path", path: "" },
      fixed: { kind: "fixed", value: "" },
      partner: { kind: "partner", key: "" },
      global: { kind: "global", setId: "", key: "" },
    };
    onChange({ from: fresh[kind] });
  };

  return (
    <div className="grid gap-2 sm:grid-cols-[minmax(0,10rem)_minmax(0,1fr)_minmax(0,8rem)]">
      <Select
        className="h-8 text-xs"
        aria-label="Where the value comes from"
        value={rule.from.kind}
        onChange={(e) => setKind(e.target.value as ValueSourceKind)}
        options={KINDS.map((k) => ({ value: k.value, label: k.label }))}
      />

      <SourceInput source={rule.from} onChange={(from) => onChange({ from })} />

      <Select
        className="h-8 text-xs"
        aria-label="Value type"
        title="What to convert the value to. A value that cannot be converted fails the exchange rather than arriving empty."
        value={rule.type ?? ""}
        onChange={(e) =>
          onChange({ type: e.target.value === "" ? undefined : (e.target.value as ValueTypeName) })
        }
        options={TYPES}
      />
    </div>
  );
}

function SourceInput({
  source,
  onChange,
}: {
  source: ValueSource;
  onChange: (source: ValueSource) => void;
}) {
  const { data: globalSets } = useQuery({
    queryKey: keys.valueSets.list,
    queryFn: () => api.listValueSets(),
    enabled: source.kind === "global",
  });

  switch (source.kind) {
    case "path":
      return (
        <TextInput
          className="h-8 font-mono text-xs"
          aria-label="Field path"
          placeholder="order.customer"
          title="A dot-separated path. It stops at a list — walk a list with a loop instead."
          value={source.path ?? ""}
          onChange={(e) => onChange({ kind: "path", path: e.target.value })}
        />
      );

    case "fixed":
      return (
        <TextInput
          className="h-8 text-xs"
          aria-label="Fixed value"
          placeholder="WEB"
          value={String(source.value ?? "")}
          onChange={(e) => onChange({ kind: "fixed", value: e.target.value })}
        />
      );

    case "partner":
      return (
        <TextInput
          className="h-8 font-mono text-xs"
          aria-label="Partner property key"
          placeholder="region-code"
          title="A key from the partner's adapter properties. Any characters are fine."
          value={source.key ?? ""}
          onChange={(e) => onChange({ kind: "partner", key: e.target.value })}
        />
      );

    case "global": {
      // The set rows carry their own values, so the key is a list to choose from
      // rather than something to type and get wrong.
      const chosenSet = globalSets?.find((s) => s.id === source.setId);
      const keyOptions = Object.keys(chosenSet?.values ?? {});

      return (
        <div className="flex gap-1.5">
          <Select
            className="h-8 min-w-0 flex-1 text-xs"
            aria-label="Global values set"
            value={source.setId ?? ""}
            onChange={(e) =>
              // A key from the old set almost certainly is not in the new one.
              onChange({ kind: "global", setId: e.target.value, key: "" })
            }
            options={[
              { value: "", label: "Choose a set…" },
              ...(globalSets ?? []).map((s) => ({ value: s.id, label: s.name || s.id })),
            ]}
          />
          {keyOptions.length > 0 ? (
            <Select
              className="h-8 min-w-0 flex-1 font-mono text-xs"
              aria-label="Global value key"
              value={source.key ?? ""}
              onChange={(e) => onChange({ ...source, kind: "global", key: e.target.value })}
              options={[
                { value: "", label: "Choose a key…" },
                ...keyOptions.map((k) => ({ value: k, label: k })),
              ]}
            />
          ) : (
            <TextInput
              className="h-8 min-w-0 flex-1 font-mono text-xs"
              aria-label="Global value key"
              placeholder={source.setId ? "This set has no values" : "key"}
              disabled={!source.setId}
              value={source.key ?? ""}
              onChange={(e) => onChange({ ...source, kind: "global", key: e.target.value })}
            />
          )}
        </div>
      );
    }
  }
}

/**
 * The transform dropdown and whatever arguments the chosen function takes.
 *
 * A closed list rather than a free-text expression, so there is no syntax to learn
 * and nothing to parse on the server. The names mirror the functions the mapper
 * implements, and a test on each side keeps the two lists in step.
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
      <Select
        className="h-8 w-44 text-xs"
        aria-label="Transform"
        value={transform?.fn ?? ""}
        onChange={(e) => onChange(e.target.value === "" ? undefined : { fn: e.target.value })}
        options={[
          { value: "", label: "No transform" },
          ...TRANSFORMS.map((t) => ({ value: t.fn, label: t.label })),
        ]}
      />

      {chosen?.args.map((arg) => (
        <TextInput
          key={arg.name}
          className="h-8 w-28 text-xs"
          aria-label={`${chosen.label} — ${arg.label}`}
          placeholder={arg.label}
          inputMode={arg.kind === "number" ? "decimal" : undefined}
          value={String(transform?.[arg.name] ?? "")}
          onChange={(e) => {
            const next: TransformRule = { ...(transform ?? { fn: chosen.fn }) };
            if (e.target.value === "") delete next[arg.name];
            else
              next[arg.name] =
                arg.kind === "number" && e.target.value.trim() !== "" && !isNaN(Number(e.target.value))
                  ? Number(e.target.value)
                  : e.target.value;
            onChange(next);
          }}
        />
      ))}
    </div>
  );
}
