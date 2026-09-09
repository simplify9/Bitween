import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Gauge, Plug, Plus, Telescope, Trash2, X } from "lucide-react";
import {
  api,
  ApiRequestError,
  SECRET_SENTINEL,
  type DataSourceInspectResult,
  type DataSourceTestResult,
} from "../../api";
import { Can, useSessionCan } from "../../auth/guards";
import { PageHeader } from "../../components/layout/PageHeader";
import { Badge, Button, FormError, LoadingBlock } from "../../components/ui/basics";
import { Checkbox, Field, PasswordInput, Select, TextInput } from "../../components/ui/forms";
import { ConfirmDialog } from "../../components/ui/overlays";
import { BackLink } from "../../components/ui/BackLink";
import { keys } from "../../api/queryKeys";
import { ConnectionBadge } from "./ConnectionBadge";
import { isSecretName, providerOf, settingOf, useDataSourceProviders } from "./providers";
import { LiveConnection } from "./LiveConnection";
import { Statements } from "./Statements";
import { draftOf, editableFingerprint, type Draft } from "./draft";

/** A draft is the whole editable surface, so the save bar can compare against what was loaded. */

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

  const providers = useDataSourceProviders();
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
  const [inspect, setInspect] = useState<DataSourceInspectResult | null>(null);
  const [removing, setRemoving] = useState(false);

  // Re-seed when the server's copy of the SETTINGS changes — saving re-masks the secrets, so the
  // form has to go back to showing "stored" rather than a value the server will never return
  // again. Not on every response: this query is polled for the connection panel, and re-seeding on
  // each poll silently threw away anything typed but not yet saved.
  const seeded = useRef<string | null>(null);
  useEffect(() => {
    if (!source.data) return;

    const fingerprint = editableFingerprint(source.data);
    if (seeded.current === fingerprint) return;

    seeded.current = fingerprint;
    setDraft(draftOf(source.data));
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
        softMemoryLimitMb: d.softMemoryLimitMb,
        hardMemoryLimitMb: d.hardMemoryLimitMb,
        cpuPercentLimit: d.cpuPercentLimit,
        cpuLimitSamples: d.cpuLimitSamples,
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

  // Discover and GetStats go to the adapter actually serving traffic, not to a throwaway instance
  // the way Test does — they are questions about the live connection.
  const ask = useMutation({
    mutationFn: (command: string) => api.inspectDataSource(dataSourceId, command),
    onSuccess: setInspect,
    onError: (e) =>
      setInspect({
        ran: false,
        command: null,
        result: null,
        error: e instanceof ApiRequestError ? e.message : "The command could not be run.",
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

  // Only ever used to illustrate what a percentage means. This is the BROWSER's core count, not
  // the node's, so it is a rough translation rather than a claim about the server.
  const cores = navigator.hardwareConcurrency || 8;
  const provider = providerOf(providers.data, d.adapterId);
  const dirty = JSON.stringify(draft) !== JSON.stringify(draftOf(d));

  const setProperty = (key: string, value: string) =>
    setDraft({ ...draft, properties: { ...draft.properties, [key]: value } });

  const removeProperty = (key: string) => {
    const properties = { ...draft.properties };
    delete properties[key];
    setDraft({ ...draft, properties });
  };

  // Declared by the adapter but not on this data source yet — offered rather than imposed, so a
  // form does not open with twenty empty boxes.
  const unused = (provider?.settings ?? [])
    .map((setting) => setting.name)
    .filter((name) => !(name in draft.properties));

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
            <Button onClick={() => ask.mutate("Discover")} disabled={ask.isPending}>
              <Telescope className="size-4" /> Discover
            </Button>
            <Button onClick={() => ask.mutate("GetStats")} disabled={ask.isPending}>
              <Gauge className="size-4" /> Stats
            </Button>
            <Can permission="data-sources.delete">
              <Button onClick={() => setRemoving(true)}>
                <Trash2 className="size-4" /> Delete
              </Button>
            </Can>
          </div>
        }
      />

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
            {source.data?.kind === "Relational"
              ? "The test runs the real adapter against these settings on its own throwaway connection, "
                + "and prepares every statement against the live schema — so a typo or a dropped column "
                + "fails here rather than on the first message."
              : "The test runs the real adapter against these settings but never consumes: the queues "
                + "its gateways read are inspected, not drained."}
          </p>
        </section>
      )}

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

        {/* The two placements are opposites, and the wrong explanation directly contradicts the
            "Held by" value right above it — which is how someone concludes the page is broken. */}
        <p className="mt-3 text-[12px] text-ink-500">
          {source.data?.kind === "Relational" ? (
            <>
              A connection pool is held by every node that runs work, not leased to one: a node
              without it could not run the exchanges that need it. Nothing here is exclusive, so
              there is no fencing token and no ownership to move.
            </>
          ) : (
            <>
              A broker connection is exclusive, so exactly one node holds it. The term after the
              node name is the fencing token — it increases every time ownership moves, and a node
              whose term is no longer current stops immediately rather than carrying on consuming.
            </>
          )}
        </p>
      </section>

      {/* ——— what it is doing right now ——— */}
      <LiveConnection dataSourceId={dataSourceId} />

      {/* Only a relational source runs SQL, and the server refuses a statement on anything else —
          so offering the panel on a broker would be offering a thing that cannot work. */}
      {source.data?.kind === "Relational" && (
        <Can permission="data-source-statements.view">
          <Statements dataSourceId={dataSourceId} />
        </Can>
      )}

      {/* ——— what the live adapter says about the broker ——— */}
      {inspect && (
        <section className="mb-4 rounded-xl border border-ink-200 bg-white p-4">
          <div className="mb-2 flex items-center gap-2">
            <h2 className="text-sm font-semibold text-ink-900">
              {inspect.command === "GetStats" ? "Adapter statistics" : "What is on the broker"}
            </h2>
            <button
              type="button"
              onClick={() => setInspect(null)}
              className="ml-auto text-[12px] text-ink-500 hover:text-ink-800"
            >
              dismiss
            </button>
          </div>

          {inspect.ran ? (
            <pre className="overflow-x-auto rounded-lg bg-ink-50 p-3 font-mono text-[12px] text-ink-800">
              {inspect.result}
            </pre>
          ) : (
            <p className="text-sm text-ink-600">{inspect.error}</p>
          )}

          <p className="mt-2 text-[12px] text-ink-500">
            Asked of the connection that is actually serving traffic, not a throwaway one — and
            read-only: nothing is consumed, acknowledged or published.
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
            <h3 className="mb-1 text-sm font-medium text-ink-800">Memory ceilings</h3>
            <p className="mb-3 text-[12px] text-ink-500">
              An adapter is a separate process holding this broker's connection. Without a ceiling
              it is bounded by nothing but the host, so one runaway payload takes every other
              integration on the node down with it. Leave both at 0 to use the host's own defaults.
            </p>

            <div className="flex flex-wrap gap-4">
              <div className="w-48">
                <Field
                  label="Soft limit (MB)"
                  htmlFor="ds-soft"
                  hint="Crossing it recycles the adapter between messages, so nothing in flight is lost."
                >
                  <TextInput
                    id="ds-soft"
                    type="number"
                    min={0}
                    value={draft.softMemoryLimitMb}
                    disabled={!canEdit}
                    onChange={(e) =>
                      setDraft({ ...draft, softMemoryLimitMb: Number(e.target.value) || 0 })
                    }
                  />
                </Field>
              </div>
              <div className="w-48">
                <Field
                  label="Hard limit (MB)"
                  htmlFor="ds-hard"
                  hint="The runtime's own ceiling: an allocation past it fails inside the adapter."
                >
                  <TextInput
                    id="ds-hard"
                    type="number"
                    min={0}
                    value={draft.hardMemoryLimitMb}
                    disabled={!canEdit}
                    onChange={(e) =>
                      setDraft({ ...draft, hardMemoryLimitMb: Number(e.target.value) || 0 })
                    }
                  />
                </Field>
              </div>
            </div>

            {draft.softMemoryLimitMb > 0 &&
              draft.hardMemoryLimitMb > 0 &&
              draft.softMemoryLimitMb > draft.hardMemoryLimitMb && (
                <p className="mt-2 text-[12px] text-danger-700">
                  A soft limit above the hard one can never be reached — the runtime fails the
                  allocation first, so the recycle never happens.
                </p>
              )}

            <p className="mt-2 text-[12px] text-ink-500">
              Applied when the adapter process launches, so changing these restarts it. Nothing in
              flight is lost: messages are only acknowledged once Bitween has persisted them.
            </p>
          </div>

          <div className="border-t border-ink-100 pt-4">
            <h3 className="mb-1 text-sm font-medium text-ink-800">CPU ceiling</h3>
            <p className="mb-3 text-[12px] text-ink-500">
              A share of the <strong>whole node</strong>, not of one core — one core pegged flat out
              on a sixteen-core node reads about 6%, so &ldquo;50%&rdquo; would allow eight cores
              rather than half of one. It trips only after several consecutive heartbeats above the
              line, because an adapter draining a backlog is <em>supposed</em> to work hard.
              Leave at 0 for the host default.
            </p>

            <div className="flex flex-wrap gap-4">
              <div className="w-48">
                <Field
                  label="CPU limit (% of node)"
                  htmlFor="ds-cpu"
                  hint={
                    draft.cpuPercentLimit > 0
                      ? `For scale: ${(draft.cpuPercentLimit / 100 * cores).toFixed(1)} core(s) on a `
                        + `${cores}-core machine — this browser's core count, not the node's.`
                      : "Off — the host default applies."
                  }
                >
                  <TextInput
                    id="ds-cpu"
                    type="number"
                    min={0}
                    max={100}
                    step={0.5}
                    value={draft.cpuPercentLimit}
                    disabled={!canEdit}
                    onChange={(e) =>
                      setDraft({ ...draft, cpuPercentLimit: Number(e.target.value) || 0 })
                    }
                  />
                </Field>
              </div>
              <div className="w-48">
                <Field
                  label="Consecutive heartbeats"
                  htmlFor="ds-cpu-samples"
                  hint="How long it must stay above the line. Higher lets a bigger burst of real work pass."
                >
                  <TextInput
                    id="ds-cpu-samples"
                    type="number"
                    min={0}
                    value={draft.cpuLimitSamples}
                    disabled={!canEdit}
                    onChange={(e) =>
                      setDraft({ ...draft, cpuLimitSamples: Number(e.target.value) || 0 })
                    }
                  />
                </Field>
              </div>
            </div>

            {draft.cpuPercentLimit > 100 && (
              <p className="mt-2 text-[12px] text-danger-700">
                Above 100% can never be reached — the figure is a share of the whole node, so 100%
                is every core at once.
              </p>
            )}

            <p className="mt-2 text-[12px] text-ink-500">
              Crossing it asks the adapter to drain rather than killing it, so in-flight messages go
              back to the broker instead of being lost.
            </p>
          </div>

          <div className="border-t border-ink-100 pt-4">
            <h3 className="mb-1 text-sm font-medium text-ink-800">Connection settings</h3>
            <p className="mb-3 text-[12px] text-ink-500">
              Handed straight to the adapter, which is also where this list comes from:{" "}
              {provider ? provider.label : d.adapterId} declares what it accepts. Anything else it
              understands can still be added by hand.
            </p>

            <div className="flex flex-col gap-3">
              {Object.keys(draft.properties).map((key) => {
                const declared = settingOf(provider, key);
                const secret = isSecretName(key, d.secretProperties, declared);
                const value = draft.properties[key];
                const stored = secret && value === SECRET_SENTINEL;

                return (
                  <div key={key} className="flex items-start gap-2">
                    <div className="flex-1">
                      <Field
                        label={declared?.required ? `${key} *` : key}
                        htmlFor={`ds-prop-${key}`}
                        hint={stored ? "Stored. Type to replace it." : declared?.hint}
                      >
                        {declared?.allowedValues ? (
                          <Select
                            id={`ds-prop-${key}`}
                            value={value}
                            disabled={!canEdit}
                            onChange={(e) => setProperty(key, e.target.value)}
                            // An empty option only where empty is legal: a setting the adapter did
                            // not mark required can be left for the broker to decide.
                            options={[
                              ...(declared.required ? [] : [{ value: "", label: "—" }]),
                              ...declared.allowedValues.map((v) => ({ value: v, label: v })),
                            ]}
                          />
                        ) : secret ? (
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
                            type={declared?.type === "number" ? "number" : "text"}
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
                    hint={
                      unused.length > 0
                        ? `${provider?.label ?? "This provider"} also accepts ${unused
                            .slice(0, 3)
                            .join(", ")}${unused.length > 3 ? ` and ${unused.length - 3} more` : ""}.`
                        : "A name that looks like a credential is masked automatically."
                    }
                  >
                    <TextInput
                      id="ds-new-key"
                      list="ds-known-settings"
                      value={newKey}
                      placeholder={unused[0] ?? "QueueType"}
                      onChange={(e) => setNewKey(e.target.value)}
                      onKeyDown={(e) => e.key === "Enter" && addProperty()}
                    />
                    {/* Typing is still allowed: an adapter may read more than it declares. */}
                    <datalist id="ds-known-settings">
                      {unused.map((name) => (
                        <option key={name} value={name} />
                      ))}
                    </datalist>
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
