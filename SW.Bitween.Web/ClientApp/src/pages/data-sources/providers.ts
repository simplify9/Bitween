import { useQuery } from "@tanstack/react-query";

import { api } from "../../api";
import { keys } from "../../api/queryKeys";
import type { DataSourceProvider, DataSourceProviderSetting } from "../../api/types";

/**
 * The bus providers this deployment can offer.
 *
 * This used to be a hand-written table right here: the fields each provider starts with, their
 * defaults, their allowed values, which of them are credentials. That is a copy of a contract the
 * front end does not own, and it drifted — a hint told operators to set "Tls" while the adapter
 * only ever read "UseSsl", so a connection that looked encrypted in this form was in the clear on
 * the wire.
 *
 * The adapter declares its own settings now and the server reads them straight out of the package,
 * so a provider added or a setting renamed shows up here without anyone editing the UI.
 */
export const useDataSourceProviders = () =>
  useQuery({
    queryKey: keys.dataSources.providers,
    queryFn: () => api.listDataSourceProviders(),
    // Only changes when an adapter is republished, which is not something a session sees.
    staleTime: 10 * 60 * 1000,
  });

export const providerOf = (
  providers: DataSourceProvider[] | undefined,
  adapterId: string,
): DataSourceProvider | undefined => providers?.find((p) => p.adapterId === adapterId);

export const settingOf = (
  provider: DataSourceProvider | undefined,
  name: string,
): DataSourceProviderSetting | undefined =>
  provider?.settings.find((s) => s.name.toLowerCase() === name.toLowerCase());

/**
 * The properties a new data source starts with: everything the adapter said is required, plus
 * everything it gave a default. The rest stays off the form until someone adds it, because a form
 * of twenty mostly-empty boxes hides the four that matter.
 */
export const initialProperties = (provider: DataSourceProvider): Record<string, string> =>
  Object.fromEntries(
    provider.settings
      .filter((s) => s.required || s.default !== null)
      .map((s) => [s.name, s.default ?? ""]),
  );

export const declaredSecrets = (provider: DataSourceProvider): string[] =>
  provider.settings.filter((s) => s.secret).map((s) => s.name);

/**
 * Whether a setting holds a credential. The adapter says so for the settings it declares, and the
 * backend masks on the same rule; this covers a property someone added by hand, before it has ever
 * been saved.
 */
const CREDENTIAL =
  /password|secret|token|credential|apikey|accesskey|privatekey|connectionstring|sas|passphrase|certificate/i;

export const isSecretName = (
  name: string,
  declared: string[] = [],
  setting?: DataSourceProviderSetting,
): boolean =>
  setting?.secret === true ||
  declared.some((d) => d.toLowerCase() === name.toLowerCase()) ||
  CREDENTIAL.test(name);
