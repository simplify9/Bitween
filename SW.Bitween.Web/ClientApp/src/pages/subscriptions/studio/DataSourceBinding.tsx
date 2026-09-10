import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { api } from "../../../api";
import { keys } from "../../../api/queryKeys";
import { Field, Select, TextInput } from "../../../components/ui/forms";

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

  // The chosen connection's own settings, for one question: does it actually define a default
  // receive mode? The dropdown used to offer "the connection's default" whether or not one
  // existed, and a subscription that took it failed at poll time with "no ReceiveMode" — a fault
  // reported nowhere near the screen that caused it.
  const chosenDetail = useQuery({
    queryKey: keys.dataSources.detail(dataSourceId ?? 0),
    queryFn: () => api.getDataSource(dataSourceId!),
    enabled: dataSourceId != null && slot === "receiver",
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

  // A receiver names its statement under a different key, and it has to be the one the adapter
  // reads: a receiver polls with ReceiveStatement, while every other slot runs Statement. Writing
  // the wrong one saves cleanly and then does nothing, which is the worst way for this to fail.
  const statementKey = slot === "receiver" ? "ReceiveStatement" : "Statement";
  const current = properties[statementKey] ?? "";

  // A statement named here but gone from the connection is the case worth surfacing: it fails at
  // run time as "not a statement this data source defines", which is a long way from this screen.
  const missing = current !== "" && !available.some((s) => s.name === current);

  const chosenStatement = available.find((s) => s.name === current) ?? null;

  // Case-insensitively, the way adapter properties bind.
  const connectionMode = Object.entries(chosenDetail.data?.properties ?? {}).find(
    ([key]) => key.toLowerCase() === "receivemode",
  )?.[1];

  const mode = properties.ReceiveMode ?? "";
  const needsCursor = ["incrementing", "timestamp", "timestamp+incrementing"].includes(mode);
  // Without a cursor, the mark statement is the only thing that stops a poll re-reading the same
  // rows for ever — so it is asked for rather than left to be discovered.
  const needsMark = mode === "bulk" || mode === "marker";

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
            if (next !== dataSourceId) setProperty(statementKey, "");
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
            hint={
              slot === "receiver"
                ? `Which of ${chosen.name}'s statements this receiver polls with. It also carries the columns that say which is the cursor and which identifies a row.`
                : `Which of ${chosen.name}'s statements this ${slot} runs. The SQL itself lives on the connection — a message supplies parameter values, never SQL.`
            }
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
              onChange={(e) => setProperty(statementKey, e.target.value)}
            />
          </Field>

          {missing && (
            <p className="text-[12px] text-danger-700">
              “{current}” is not a statement {chosen.name} defines any more. This subscription will
              fail on its next message until it names one that exists.
            </p>
          )}

          {slot !== "receiver" && (
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
          )}

          {/*
            A receiver has no operation to pick — polling is the operation. What it has instead is
            a reading policy, and that is this subscription's, not the connection's: the same
            statement is legitimately read in bulk once for a backfill and incrementally after.

            What is NOT here is the cursor and key columns. They describe the rows the statement
            returns, so they live on the statement, where every subscription polling it agrees on
            them rather than each nominating its own.
          */}
          {slot === "receiver" && (
            <>
              <Field
                label="Receive mode"
                hint="How this subscription finds new rows. None of these can see a DELETE — that is a property of polling, not of this adapter."
              >
                <Select
                  value={properties.ReceiveMode ?? ""}
                  disabled={disabled}
                  options={[
                    {
                      value: "",
                      label: connectionMode
                        ? `The connection's default — ${connectionMode}`
                        : "None — and this connection sets no default",
                    },
                    { value: "incrementing", label: "incrementing — follow an always-growing column" },
                    { value: "timestamp", label: "timestamp — follow a modified-at column" },
                    {
                      value: "timestamp+incrementing",
                      label: "timestamp+incrementing — both, for rows sharing a timestamp",
                    },
                    { value: "bulk", label: "bulk — re-read everything each poll" },
                    { value: "marker", label: "marker — rows a flag says are unprocessed" },
                  ]}
                  onChange={(e) => setProperty("ReceiveMode", e.target.value)}
                />
              </Field>

              {mode === "" && !connectionMode && !chosenDetail.isLoading && (
                <p className="text-[12px] text-warn-700">
                  Nothing here or on the connection says how to find new rows, so this receiver
                  will fail on its first poll. Pick a mode.
                </p>
              )}

              {needsMark && (
                <Field
                  label="Mark processed"
                  hint="Run against each row once Bitween has accepted it: set a flag, move the row, delete it. Bind the row's key as the key parameter."
                >
                  <Select
                    value={properties.MarkProcessedStatement ?? ""}
                    disabled={disabled || statements.isLoading}
                    options={[
                      { value: "", label: "None — the connection's own, if it has one" },
                      ...available.map((s) => ({ value: s.name, label: s.name })),
                    ]}
                    onChange={(e) => setProperty("MarkProcessedStatement", e.target.value)}
                  />
                </Field>
              )}

              {needsMark && !properties.MarkProcessedStatement && (
                <p className="text-[12px] text-warn-700">
                  {properties.ReceiveMode} has no cursor, so without a mark-processed statement
                  every poll reads the same rows again, forever.
                </p>
              )}

              <Field
                label="Batch size"
                hint="Rows one poll may take. The next poll takes the next batch. Blank uses the connection's default."
              >
                <TextInput
                  type="number"
                  min={1}
                  value={properties.ReceiveBatchSize ?? ""}
                  disabled={disabled}
                  onChange={(e) => setProperty("ReceiveBatchSize", e.target.value)}
                />
              </Field>

              {chosenStatement && !chosenStatement.keyColumn && (
                <p className="text-[12px] text-warn-700">
                  “{chosenStatement.name}” does not say which of its columns identifies a row, so
                  polling it will fail. Set its key column on the connection.
                </p>
              )}

              {chosenStatement && needsCursor && !chosenStatement.cursorColumn && (
                <p className="text-[12px] text-warn-700">
                  {properties.ReceiveMode} follows a column, but “{chosenStatement.name}” does not
                  say which of its columns is the cursor.
                </p>
              )}
            </>
          )}

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
