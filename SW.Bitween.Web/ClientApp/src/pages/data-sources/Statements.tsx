import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router";
import { Plus, Trash2 } from "lucide-react";
import { api, type DataSourceStatement } from "../../api";
import { keys } from "../../api/queryKeys";
import { Can, useSessionCan } from "../../auth/guards";
import { Badge, Button, FormError, LoadingBlock } from "../../components/ui/basics";
import { Checkbox, Field, TextInput } from "../../components/ui/forms";
import { ConfirmDialog } from "../../components/ui/overlays";
import { Panel } from "../../components/ui/Panel";

/**
 * The SQL this connection is allowed to run.
 *
 * It lives here, on the data source, rather than as its own page in the sidebar — a statement is
 * only ever reached through the connection it runs against, the same reason data sources sit under
 * bus gateways in the nav.
 *
 * But it is NOT gated on the data source's own permission. Writing a query and rotating a database
 * password are different jobs, and `data-sources.edit` is the one that changes credentials — so
 * these controls answer to `data-source-statements.*` instead. That separation is the whole reason
 * a statement is a record rather than a field.
 */
export function Statements({ dataSourceId }: { dataSourceId: number }) {
  const queryClient = useQueryClient();
  const canCreate = useSessionCan("data-source-statements.create");
  const canEdit = useSessionCan("data-source-statements.edit");

  const [error, setError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [removing, setRemoving] = useState<DataSourceStatement | null>(null);

  const statements = useQuery({
    queryKey: keys.dataSourceStatements.forDataSource(dataSourceId),
    queryFn: () => api.listDataSourceStatements(dataSourceId),
    retry: false,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({
      queryKey: keys.dataSourceStatements.forDataSource(dataSourceId),
    });

  const remove = useMutation({
    mutationFn: (id: number) => api.deleteDataSourceStatement(id),
    onSuccess: () => {
      setRemoving(null);
      setError(null);
      void invalidate();
    },
    // The server refuses while anything names it, and the refusal lists what. That message is the
    // useful part, so it is shown as-is rather than replaced with something generic.
    onError: (e: Error) => setError(e.message),
  });

  if (statements.isLoading) return <LoadingBlock />;

  const rows = statements.data ?? [];

  return (
    <Panel
      title="SQL statements"
      description="The named SQL this connection may run. A subscription names one; the SQL never travels with a message."
      action={
        <Can permission="data-source-statements.create">
          <Button variant="secondary" onClick={() => setAdding(true)}>
            <Plus className="size-4" /> Add statement
          </Button>
        </Can>
      }
    >
      {error && <div className="mx-4 mb-3"><FormError>{error}</FormError></div>}

      {rows.length === 0 ? (
        <p className="px-4 pb-4 text-[13px] text-ink-500">
          None yet. Until one exists, a subscription bound to this connection has nothing it is
          allowed to run — the adapter refuses SQL sent with a message unless ad-hoc SQL is
          explicitly switched on.
        </p>
      ) : (
        <ul className="divide-y divide-ink-100 border-t border-ink-100">
          {rows.map((statement) => (
            <StatementRow
              key={statement.id}
              statement={statement}
              canEdit={canEdit}
              onDelete={() => {
                setError(null);
                setRemoving(statement);
              }}
              onSaved={invalidate}
              onError={setError}
            />
          ))}
        </ul>
      )}

      {adding && canCreate && (
        <StatementForm
          dataSourceId={dataSourceId}
          onClose={() => setAdding(false)}
          onSaved={() => {
            setAdding(false);
            void invalidate();
          }}
          onError={setError}
        />
      )}

      {removing && (
        <ConfirmDialog
          title={`Delete ${removing.name}?`}
          confirmLabel="Delete"
          body={
            removing.usageCount > 0
              ? `${removing.usageCount} subscription(s) name this statement. The server will refuse — mark it inactive instead, to retire it while they are migrated.`
              : "Nothing names this statement, so removing it changes no running integration."
          }
          onClose={() => setRemoving(null)}
          onConfirm={() => remove.mutateAsync(removing.id).then(() => undefined)}
        />
      )}
    </Panel>
  );
}

/**
 * One statement. The usage count is the column that matters: it is the difference between SQL that
 * is dead and SQL that is merely quiet, and it is what stopped anyone ever tidying up the JSON
 * field this replaced.
 */
function StatementRow({
  statement,
  canEdit,
  onDelete,
  onSaved,
  onError,
}: {
  statement: DataSourceStatement;
  canEdit: boolean;
  onDelete: () => void;
  onSaved: () => void;
  onError: (message: string) => void;
}) {
  const [open, setOpen] = useState(false);

  const usage = useQuery({
    queryKey: keys.dataSourceStatements.usage(statement.id),
    queryFn: () => api.getDataSourceStatementUsage(statement.id),
    // Only when the row is expanded: the answer costs a scan of this connection's subscriptions,
    // and a list of forty statements would run forty of them on first paint.
    enabled: open,
    retry: false,
  });

  return (
    <li className="px-4 py-3">
      <div className="flex items-start justify-between gap-3">
        <button
          type="button"
          className="min-w-0 flex-1 text-left"
          onClick={() => setOpen(!open)}
          aria-expanded={open}
        >
          <span className="flex flex-wrap items-center gap-2">
            <code className="text-[13px] font-semibold text-ink-900">{statement.name}</code>
            {statement.inactive && <Badge tone="warn">Inactive</Badge>}
            <Badge tone={statement.usageCount > 0 ? "ink" : "neutral"}>
              {statement.usageCount === 0
                ? "Unused"
                : `${statement.usageCount} subscription${statement.usageCount === 1 ? "" : "s"}`}
            </Badge>
            {statement.workGroupName && <Badge tone="neutral">{statement.workGroupName}</Badge>}
          </span>
          {statement.description && (
            <p className="mt-0.5 text-[13px] text-ink-500">{statement.description}</p>
          )}
          <code className="mt-1 block truncate text-[12px] text-ink-400">{statement.sql}</code>
        </button>

        <Can permission="data-source-statements.delete">
          <Button variant="ghost" aria-label={`Delete ${statement.name}`} onClick={onDelete}>
            <Trash2 className="size-4" />
          </Button>
        </Can>
      </div>

      {open && (
        <div className="mt-3 space-y-3 border-t border-ink-100 pt-3">
          <pre className="overflow-x-auto rounded-lg bg-ink-50 p-3 text-[12px] text-ink-800">
            {statement.sql}
          </pre>

          <div className="text-[12px] text-ink-500">
            {usage.isLoading && "Checking what uses this…"}
            {usage.data && usage.data.usedBy.length === 0 && (
              <span>
                Nothing names it. Safe to change or delete — though a subscription could be added
                tomorrow, so a name that reads like what it does is still worth having.
              </span>
            )}
            {usage.data && usage.data.usedBy.length > 0 && (
              <>
                <span className="block font-medium text-ink-700">Used by</span>
                <ul className="mt-1 space-y-0.5">
                  {usage.data.usedBy.map((entry) => (
                    <li key={`${entry.subscriptionId}-${entry.role}`}>
                      <Link
                        className="text-accent-600 hover:underline"
                        to={`/subscriptions/${entry.subscriptionId}`}
                      >
                        {entry.subscriptionName}
                      </Link>{" "}
                      — {entry.role}, {entry.operation}
                      {entry.inactive && " (inactive)"}
                    </li>
                  ))}
                </ul>
                <p className="mt-1">
                  Renaming is refused while any of these name it: a rename would break them, and
                  nothing on this screen can repair that. Changing the SQL is allowed.
                </p>
              </>
            )}
          </div>

          {canEdit && (
            <StatementForm
              statement={statement}
              dataSourceId={statement.dataSourceId}
              onClose={() => setOpen(false)}
              onSaved={onSaved}
              onError={onError}
            />
          )}
        </div>
      )}
    </li>
  );
}

function StatementForm({
  statement,
  dataSourceId,
  onClose,
  onSaved,
  onError,
}: {
  statement?: DataSourceStatement;
  dataSourceId: number;
  onClose: () => void;
  onSaved: () => void;
  onError: (message: string) => void;
}) {
  const [name, setName] = useState(statement?.name ?? "");
  const [sql, setSql] = useState(statement?.sql ?? "");
  const [description, setDescription] = useState(statement?.description ?? "");
  const [inactive, setInactive] = useState(statement?.inactive ?? false);

  const save = useMutation({
    mutationFn: async () => {
      if (statement) {
        await api.updateDataSourceStatement(statement.id, {
          name,
          sql,
          description,
          workGroupId: statement.workGroupId,
          inactive,
        });
      } else {
        await api.createDataSourceStatement(dataSourceId, { name, sql, description });
      }
    },
    onSuccess: onSaved,
    onError: (e: Error) => onError(e.message),
  });

  const complete = name.trim().length > 0 && sql.trim().length > 0;

  return (
    <div className="space-y-3 rounded-lg border border-ink-200 bg-ink-50/50 p-3">
      <Field
        label="Name"
        hint="What a subscription puts in its Statement property. Unique per connection, and matched without regard to case."
      >
        <TextInput value={name} onChange={(e) => setName(e.target.value)} />
      </Field>

      <Field
        label="SQL"
        hint="Bind values as parameters — :name on Oracle, @name on PostgreSQL. Never concatenate a value into the text."
      >
        <textarea
          className="min-h-28 w-full rounded-lg border border-ink-200 bg-white px-3 py-2 font-mono text-[13px] text-ink-900"
          value={sql}
          onChange={(e) => setSql(e.target.value)}
          spellCheck={false}
        />
      </Field>

      <Field label="Description" hint="Why it exists, for whoever inherits it.">
        <TextInput value={description} onChange={(e) => setDescription(e.target.value)} />
      </Field>

      {statement && (
        <Checkbox
          checked={inactive}
          onChange={(e) => setInactive(e.target.checked)}
          label="Inactive"
          description="Kept, but dropped from what the adapter will run. A subscription still naming it fails loudly, which is the point of retiring rather than deleting."
        />
      )}

      <div className="flex gap-2">
        <Button onClick={() => save.mutate()} disabled={!complete || save.isPending}>
          {statement ? "Save changes" : "Create statement"}
        </Button>
        <Button variant="ghost" onClick={onClose}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
