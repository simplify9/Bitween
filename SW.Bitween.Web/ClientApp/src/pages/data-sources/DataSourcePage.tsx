import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Plug, Plus, Trash2, X } from "lucide-react";
import { api, ApiRequestError, SECRET_SENTINEL, type DataSourceDetail, type DataSourceTestResult } from "../../api";
import { Can, useSessionCan } from "../../auth/guards";
import { PageHeader } from "../../components/layout/PageHeader";
import { Badge, Button, FormError, LoadingBlock } from "../../components/ui/basics";
import { Checkbox, Field, PasswordInput, TextInput } from "../../components/ui/forms";
import { ConfirmDialog } from "../../components/ui/overlays";
import { BackLink } from "../../components/ui/BackLink";
import { keys } from "../../api/queryKeys";
import { ConnectionBadge } from "./ConnectionBadge";
import { isSecretName, providerOf } from "./providers";
import { LiveConnection } from "./LiveConnection";

/** A draft is the whole editable surface, so the save bar can compare against what was loaded. */
interface Draft {
  name: string;
  inactive: boolean;
  deduplicationWindowDays: number;
  properties: Record<string, string>;
}

const draftOf = (d: DataSourceDetail): Draft => ({
  name: d.name,
  inactive: d.inactive,
  deduplicationWindowDays: d.deduplicationWindowDays,
  properties: { ...d.properties },
});

/**
 * One connection: its settings, whether it works, and who is holding it.
 *
 * The Test button carries more weight than it looks. Without it the only way to discover a wrong
 * password is to save, wait up to thirty seconds for the supervisor to reconcile, and then read the
 * health row — so a typo costs a minute and a guess at which of six fields caused it.
 */
export function DataSourcePage() {
  const { id = "" } = useParams();
  const dataSourceId = Number(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const canEdit = useSessionCan("data-sources.edit");

  const source = useQuery({
    queryKey: keys.dataSources.detail(dataSourceId),
    queryFn: () => api.getDataSource(dataSourceId),
    retry: false,
    refetchInterval: 10_000,
  });

  const [draft, setDraft] = useState<Draft | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<DataSourceTestResult | null>(null);
  const [newKey, setNewKey] = useState("");
  const [removing, setRemoving] = useState(false);

  // Re-seed whenever the server's copy changes: saving re-masks the secrets, so the form has to
  // go back to showing "stored" rather than a value the server will never return again.
  useEffect(() => {
    if (source.data) setDraft(draftOf(source.data));
  }, [source.data]);

  const save = useMutation({
    mutationFn: (d: Draft) =>
      api.updateDataSource(dataSourceId, {
        name: d.name,
        adapterId: source.data!.adapterId,
        kind: source.data!.kind,
        properties: d.properties,
        secretProperties: source.data!.secretProperties,
        inactive: d.inactive,
        deduplicationWindowDays: d.deduplicationWindowDays,
      }),
    onSuccess: async () => {
      setError(null);
      await queryClient.invalidateQueries({ queryKey: keys.dataSources.all });
    },
    onError: (e) => setError(e instanceof ApiRequestError ? e.message : "Could not save."),
  });

  const test = useMutation({
    mutationFn: () => api.testDataSource(dataSourceId),
    onSuccess: setResult,
    onError: (e) =>
      setResult({
        succeeded: false,
        error: e instanceof ApiRequestError ? e.message : "The test could not be run.",
        stages: [],
      }),
  });

  const remove = useMutation({
    mutationFn: () => api.deleteDataSource(dataSourceId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: keys.dataSources.all });
      navigate("/data-sources");
    },
    onError: (e) => {
      setRemoving(false);
      setError(e instanceof ApiRequestError ? e.message : "Could not delete this data source.");
    },
  });

  if (source.isPending) return <LoadingBlock label="Loading data source…" />;
  if (source.isError || !source.data || !draft)
    return <FormError>This data source no longer exists.</FormError>;

  const d = source.data;
  const provider = providerOf(d.adapterId);
  const dirty = JSON.stringify(draft) !== JSON.stringify(draftOf(d));

  const setProperty = (key: string, value: string) =>
    setDraft({ ...draft, properties: { ...draft.properties, [key]: value } });

  const removeProperty = (key: string) => {
    const properties = { ...draft.properties };
    delete properties[key];
    setDraft({ ...draft, properties });
  };

  const addProperty = () => {
    const key = newKey.trim();
    if (!key || key in draft.properties) return;
    setProperty(key, "");
    setNewKey("");
  };

  return (
    <div className="max-w-3xl">
      {removing && (
        <ConfirmDialog
          title={`Delete "${d.name}"?`}
          confirmLabel="Delete"
          onClose={() => setRemoving(false)}
          onConfirm={() => remove.mutateAsync()}
          body={
            d.gatewayCount > 0
              ? `${d.gatewayCount} bus gateway(s) still read from this connection, so deleting will be refused until they are moved off it.`
              : "Its remembered deduplication keys go with it, so a message already processed could be handled again if it arrives later."
          }
        />
      )}

      <BackLink to="/data-sources" label="Data sources" className="mb-3" />

      <PageHeader
        title={d.name}
        description={provider?.description ?? d.adapterId}
        actions={
          <div className="flex items-center gap-2">
            <Can permission="data-sources.operate">
              <Button onClick={() => test.mutate()} disabled={test.isPending}>
                <Plug className="size-4" /> {test.isPending ? "Testing…" : "Test connection"}
              </Button>
            </Can>
            <Can permission="data-sources.delete">
              <Button onClick={() => setRemoving(true)}>
                <Trash2 className="size-4" /> Delete
              </Button>
            </Can>
          </div>
        }
      />

      {/* ——— what the last heartbeat said ——— */}
      <section className="mb-4 rounded-xl border border-ink-200 bg-white p-4">
        <div className="mb-3 flex items-center gap-3">
          <h2 className="text-sm font-semibold text-ink-900">Connection</h2>
          <ConnectionBadge state={d.lastKnownState} failures={d.consecutiveFailures} />
        </div>

        <dl className="grid grid-cols-1 gap-x-8 gap-y-2 text-sm sm:grid-cols-2">
          <Row label="Held by">{d.ownedByNode ?? "Not running on any node"}</Row>
          <Row label="Last heartbeat">
            {d.lastHeartbeatOn ? new Date(d.lastHeartbeatOn).toLocaleString() : "—"}
          </Row>
          <Row label="Consecutive restarts">{String(d.consecutiveFailures)}</Row>
          <Row label="Bus gateways reading this">
            <Link to="/bus-gateways" className="text-crimson-700 hover:underline">
              {d.gatewayCount}
            </Link>
          </Row>
          {d.lastException && (
            <div className="sm:col-span-2">
              <dt className="text-ink-500">Last error</dt>
              <dd className="text-danger-700">{d.lastException}</dd>
            </div>
          )}
        </dl>

        <p className="mt-3 text-[12px] text-ink-500">
          A broker connection is exclusive, so exactly one node holds it. The term after the node
          name is the fencing token — it increases every time ownership moves, and a node whose term
          is no longer current stops immediately rather than carrying on consuming.
        </p>
      </section>

      {/* ——— what it is doing right now ——— */}
      <LiveConnection dataSourceId={dataSourceId} />

      {/* ——— the test's answer ——— */}
      {result && (
        <section
          className={`mb-4 rounded-xl border p-4 ${
            result.succeeded ? "border-ok-200 bg-ok-50" : "border-danger-200 bg-danger-50"
          }`}
        >
          <div className="mb-2 flex items-center gap-2">
            <h2 className="text-sm font-semibold text-ink-900">
              {result.succeeded ? "The connection works" : "The connection failed"}
            </h2>
            <Badge tone={result.succeeded ? "ok" : "danger"}>{result.succeeded ? "OK" : "Failed"}</Badge>
          </div>

          <ul className="flex flex-col gap-1">
            {result.stages.map((stage, i) => (
              <li key={i} className="flex items-start gap-2 text-sm">
                {stage.succeeded ? (
                  <Check className="mt-0.5 size-4 shrink-0 text-ok-700" />
                ) : (
                  <X className="mt-0.5 size-4 shrink-0 text-danger-700" />
                )}
                <span className="w-56 shrink-0 font-medium text-ink-800">{stage.name}</span>
                <span className="text-ink-600">{stage.detail}</span>
              </li>
            ))}
          </ul>

          {!result.succeeded && result.error && (
            <p className="mt-2 text-sm text-danger-800">{result.error}</p>
          )}

          <p className="mt-2 text-[12px] text-ink-500">
            The test runs the real adapter against these settings but never consumes: the queues its
            gateways read are inspected, not drained.
          </p>
        </section>
      )}

      {/* ——— settings ——— */}
      <section className="rounded-xl border border-ink-200 bg-white p-5">
        <h2 className="mb-4 text-sm font-semibold text-ink-900">Settings</h2>

        <div className="flex flex-col gap-4">
          <Field label="Name" htmlFor="ds-name">
            <TextInput
              id="ds-name"
              value={draft.name}
              disabled={!canEdit}
              onChange={(e) => setDraft({ ...draft, name: e.target.value })}
            />
          </Field>

          <Checkbox
            checked={!draft.inactive}
            disabled={!canEdit}
            onChange={(e) => setDraft({ ...draft, inactive: !e.target.checked })}
            label="Active"
            description="Turning this off stops the connection without losing its settings."
          />

          <Field
            label="Remember deduplication keys for (days)"
            htmlFor="ds-dedupe"
            hint="Has to exceed the widest redelivery window this broker can produce — its message TTL, a dead-letter replay, someone re-driving a queue by hand. A key forgotten too early lets a redelivery through as a fresh message. Zero turns deduplication off."
          >
            <TextInput
              id="ds-dedupe"
              type="number"
              min={0}
              value={draft.deduplicationWindowDays}
              disabled={!canEdit}
              onChange={(e) =>
                setDraft({ ...draft, deduplicationWindowDays: Number(e.target.value) || 0 })
              }
            />
          </Field>

          <div className="border-t border-ink-100 pt-4">
            <h3 className="mb-1 text-sm font-medium text-ink-800">Connection settings</h3>
            <p className="mb-3 text-[12px] text-ink-500">
              Handed straight to the adapter. Bitween does not model any broker's topology, so
              anything the provider understands can go here.
            </p>

            <div className="flex flex-col gap-3">
              {Object.keys(draft.properties).map((key) => {
                const secret = isSecretName(key, d.secretProperties);
                const value = draft.properties[key];
                const stored = secret && value === SECRET_SENTINEL;

                return (
                  <div key={key} className="flex items-start gap-2">
                    <div className="flex-1">
                      <Field
                        label={key}
                        htmlFor={`ds-prop-${key}`}
                        hint={stored ? "Stored. Type to replace it." : provider?.hints[key]}
                      >
                        {secret ? (
                          <PasswordInput
                            id={`ds-prop-${key}`}
                            value={stored ? "" : value}
                            disabled={!canEdit}
                            placeholder={stored ? "••••••••" : undefined}
                            // Typing replaces the secret outright: appending to a sentinel that is
                            // not the password would save something nobody chose.
                            onChange={(e) => setProperty(key, e.target.value)}
                          />
                        ) : (
                          <TextInput
                            id={`ds-prop-${key}`}
                            value={value}
                            disabled={!canEdit}
                            onChange={(e) => setProperty(key, e.target.value)}
                          />
                        )}
                      </Field>
                    </div>
                    {canEdit && (
                      <button
                        type="button"
                        title={`Remove ${key}`}
                        onClick={() => removeProperty(key)}
                        className="mt-7 rounded-lg p-1.5 text-ink-400 hover:bg-ink-100 hover:text-danger-700"
                      >
                        <Trash2 className="size-4" />
                      </button>
                    )}
                  </div>
                );
              })}
            </div>

            {canEdit && (
              <div className="mt-3 flex items-end gap-2">
                <div className="w-64">
                  <Field
                    label="Add a setting"
                    htmlFor="ds-new-key"
                    hint="A name that looks like a credential is masked automatically."
                  >
                    <TextInput
                      id="ds-new-key"
                      value={newKey}
                      placeholder="QueueType"
                      onChange={(e) => setNewKey(e.target.value)}
                      onKeyDown={(e) => e.key === "Enter" && addProperty()}
                    />
                  </Field>
                </div>
                <Button onClick={addProperty} disabled={!newKey.trim()}>
                  <Plus className="size-4" /> Add
                </Button>
              </div>
            )}
          </div>

          {error && <FormError>{error}</FormError>}

          {canEdit && (
            <div className="flex items-center gap-2 border-t border-ink-100 pt-4">
              <Button
                variant="primary"
                onClick={() => save.mutate(draft)}
                disabled={!dirty || save.isPending}
              >
                {save.isPending ? "Saving…" : "Save"}
              </Button>
              {dirty && (
                <Button onClick={() => setDraft(draftOf(d))} disabled={save.isPending}>
                  Discard
                </Button>
              )}
              {!dirty && !save.isPending && <span className="text-sm text-ink-500">No changes.</span>}
            </div>
          )}
        </div>
      </section>
    </div>
  );
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-ink-500">{label}</dt>
      <dd className="text-ink-900">{children}</dd>
    </div>
  );
}
