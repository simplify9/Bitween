import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { api } from "../../../api";
import { keys } from "../../../api/queryKeys";
import { Field, Select } from "../../../components/ui/forms";

/**
 * Binds an adapter slot to a database: which connection, which statement, and what to do with it.
 *
 * Shown only when the slot's adapter is a relational data source provider, because for anything
 * else there is nothing here to choose. It writes two things, in two places, and the split is the
 * point:
 *
 * - The CONNECTION is the subscription's (`dataSourceId`). One per subscription — several slots
 *   pointed at the same database share one warm pool, which is the whole reason the pool is held
 *   by a long-lived process.
 * - The STATEMENT and OPERATION are this SLOT's adapter properties, so a subscription's handler
 *   and its receiver can run different statements through the same connection.
 *
 * What it never offers is a box to type SQL into. Adapter property values have `{{partner.X}}`
 * substituted into them before the adapter sees them, so SQL here would be steerable by ordinary
 * partner data. The statement is a name; the SQL lives on the data source, under its own permission.
 */
export function DataSourceBinding({
  dataSourceId,
  properties,
  onDataSourceChange,
  onPropertiesChange,
  disabled,
  slot,
}: {
  dataSourceId: number | null;
  properties: Record<string, string>;
  onDataSourceChange: (id: number | null) => void;
  onPropertiesChange: (properties: Record<string, string>) => void;
  disabled?: boolean;
  /** Named in the hints, so it is clear which stage's statement is being chosen. */
  slot: "handler" | "mapper" | "receiver";
}) {
  const sources = useQuery({
    queryKey: keys.dataSources.list,
    queryFn: () => api.listDataSources(),
    staleTime: 60_000,
  });

  const statements = useQuery({
    queryKey: keys.dataSourceStatements.forDataSource(dataSourceId ?? 0),
    queryFn: () => api.listDataSourceStatements(dataSourceId!),
    enabled: dataSourceId != null,
    staleTime: 30_000,
  });

  // Only a relational source can run SQL, and the server refuses a statement on anything else —
  // so offering a broker here would be offering something that cannot work.
  const relational = (sources.data ?? []).filter((s) => s.kind === "Relational");
  const chosen = relational.find((s) => s.id === dataSourceId) ?? null;

  const setProperty = (name: string, value: string) => {
    const next = { ...properties };
    if (value === "") delete next[name];
    else next[name] = value;
    onPropertiesChange(next);
  };

  const available = (statements.data ?? []).filter((s) => !s.inactive);
  const current = properties.Statement ?? "";

  // A statement named here but gone from the connection is the case worth surfacing: it fails at
  // run time as "not a statement this data source defines", which is a long way from this screen.
  const missing = current !== "" && !available.some((s) => s.name === current);

  return (
    <div className="space-y-3 rounded-lg border border-ink-200 bg-ink-50/50 p-3">
      <Field
        label="Connection"
        hint="The database this runs through. Shared by every stage of this subscription, so they use one pooled connection rather than opening their own."
      >
        <Select
          value={dataSourceId == null ? "" : String(dataSourceId)}
          disabled={disabled || sources.isLoading}
          options={[
            { value: "", label: "None — this adapter carries its own settings" },
            ...relational.map((s) => ({ value: String(s.id), label: s.name })),
          ]}
          onChange={(e) => {
            const next = e.target.value === "" ? null : Number(e.target.value);
            onDataSourceChange(next);

            // The statement belonged to the old connection; statement names are scoped to their
            // data source, so keeping it would point at something that may not exist here.
            if (next !== dataSourceId) setProperty("Statement", "");
          }}
        />
      </Field>

      {relational.length === 0 && !sources.isLoading && (
        <p className="text-[12px] text-ink-500">
          No database data sources exist yet.{" "}
          <Link className="text-accent-600 hover:underline" to="/data-sources">
            Create one
          </Link>{" "}
          and give it the SQL this subscription should run.
        </p>
      )}

      {chosen && (
        <>
          <Field
            label="Statement"
            hint={`Which of ${chosen.name}'s statements this ${slot} runs. The SQL itself lives on the connection — a message supplies parameter values, never SQL.`}
          >
            <Select
              value={current}
              disabled={disabled || statements.isLoading}
              options={[
                { value: "", label: "None — the message must name one itself" },
                ...available.map((s) => ({
                  value: s.name,
                  label: s.description ? `${s.name} — ${s.description}` : s.name,
                })),
                // Kept selectable so choosing something else is a decision, not an accident.
                ...(missing ? [{ value: current, label: `${current} — no longer on this connection` }] : []),
              ]}
              onChange={(e) => setProperty("Statement", e.target.value)}
            />
          </Field>

          {missing && (
            <p className="text-[12px] text-danger-700">
              “{current}” is not a statement {chosen.name} defines any more. This subscription will
              fail on its next message until it names one that exists.
            </p>
          )}

          <Field
            label="Operation"
            hint="query returns rows; execute reports what it changed; call runs a stored procedure."
          >
            <Select
              value={properties.Operation ?? "query"}
              disabled={disabled}
              options={[
                { value: "query", label: "query" },
                { value: "execute", label: "execute" },
                { value: "call", label: "call" },
              ]}
              onChange={(e) => setProperty("Operation", e.target.value)}
            />
          </Field>

          <p className="text-[12px] text-ink-500">
            <Link className="text-accent-600 hover:underline" to={`/data-sources/${chosen.id}`}>
              Manage {chosen.name}&apos;s statements
            </Link>
          </p>
        </>
      )}
    </div>
  );
}
