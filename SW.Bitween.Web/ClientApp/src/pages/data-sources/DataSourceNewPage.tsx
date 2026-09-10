import { useState } from "react";
import { useNavigate } from "react-router";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { api, ApiRequestError } from "../../api";
import { PageHeader } from "../../components/layout/PageHeader";
import { Button, FormError, LoadingBlock } from "../../components/ui/basics";
import { Field, Select, TextInput } from "../../components/ui/forms";
import { BackLink } from "../../components/ui/BackLink";
import { keys } from "../../api/queryKeys";
import { declaredSecrets, initialProperties, useDataSourceProviders } from "./providers";

/**
 * Creating asks for a name and a provider, and nothing else.
 *
 * The connection settings depend on the provider — RabbitMQ wants a virtual host, SQS wants a
 * region — so asking for them before that is chosen means either the wrong fields or a blank
 * key/value grid. The provider seeds its own, and the next screen is a form to fill in.
 *
 * Which providers exist, and what each one starts with, comes from the adapters themselves.
 */
export function DataSourceNewPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [name, setName] = useState("");
  const [adapterId, setAdapterId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const providers = useDataSourceProviders();
  // Nothing is chosen until the catalog arrives, so the first provider stands in for a choice the
  // operator has not made yet.
  const provider = providers.data?.find((p) => p.adapterId === adapterId) ?? providers.data?.[0];

  const create = useMutation({
    mutationFn: () => {
      if (!provider) throw new Error("No provider chosen.");
      return api.createDataSource({
        name: name.trim(),
        adapterId: provider.adapterId,
        kind: provider.kind,
        properties: initialProperties(provider),
        secretProperties: declaredSecrets(provider),
      });
    },
    onSuccess: async ({ id }) => {
      await queryClient.invalidateQueries({ queryKey: keys.dataSources.all });
      navigate(`/data-sources/${id}`);
    },
    onError: (e) =>
      setError(e instanceof ApiRequestError ? e.message : "Could not create this data source."),
  });

  const submit = () => {
    setError(null);
    if (!name.trim()) {
      setError("A name is required.");
      return;
    }
    create.mutate();
  };

  if (providers.isPending) return <LoadingBlock label="Loading providers…" />;

  // No adapter in the store declares itself a data source provider. Publishing one is the fix,
  // and saying so is more use than an empty menu.
  if (!provider)
    return (
      <div className="max-w-xl">
        <BackLink to="/data-sources" label="Data sources" className="mb-3" />
        <FormError>
          No data source provider adapters are installed. Publish one — bitween.bus.rabbitmq and
          bitween.bus.sqs ship with Bitween — and it will appear here.
        </FormError>
      </div>
    );

  return (
    <div className="max-w-xl">
      <BackLink to="/data-sources" label="Data sources" className="mb-3" />
      <PageHeader title="New data source" description="A connection to a broker outside Bitween." />

      <div className="flex flex-col gap-4 rounded-xl border border-ink-200 bg-white p-5">
        <Field label="Name" htmlFor="ds-name">
          <TextInput
            id="ds-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Acme's RabbitMQ"
            autoFocus
          />
        </Field>

        <Field
          label="Provider"
          htmlFor="ds-provider"
          hint={provider.description ?? provider.adapterId}
        >
          <Select
            id="ds-provider"
            value={provider.adapterId}
            onChange={(e) => setAdapterId(e.target.value)}
            options={(providers.data ?? []).map((p) => ({ value: p.adapterId, label: p.label }))}
          />
        </Field>

        {error && <FormError>{error}</FormError>}

        <div className="flex items-center gap-2">
          <Button variant="primary" onClick={submit} disabled={create.isPending}>
            {create.isPending ? "Creating…" : "Create"}
          </Button>
          <Button onClick={() => navigate("/data-sources")}>Cancel</Button>
        </div>
      </div>
    </div>
  );
}
