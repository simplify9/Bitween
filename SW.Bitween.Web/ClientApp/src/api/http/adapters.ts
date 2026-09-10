import type { ApiClient } from "../client";
import type { AdapterInfo, AdapterKind, AdapterProp } from "../types";
import { get } from "./request";

interface RawStartupValue {
  optional: boolean;
  default: string | null;
  private: boolean;
  description: string | null;
}
interface RawCatalogAdapter {
  key: string;
  native: boolean;
  versions: string[] | null;
  startupValues: Record<string, RawStartupValue> | null;
}

// The backend's Prefix param takes the plural, lowercase form.
const KIND_PREFIX: Record<AdapterKind, string> = {
  receiver: "receivers",
  handler: "handlers",
  mapper: "mappers",
  validator: "validators",
};

function toProps(values: Record<string, RawStartupValue> | null): AdapterProp[] {
  return Object.entries(values ?? {}).map(([key, v]) => ({
    key,
    optional: v.optional,
    default: v.default ?? undefined,
    secret: v.private,
    description: v.description ?? undefined,
  }));
}

export const adapterMethods = {
  async listAdapters(kind: AdapterKind): Promise<AdapterInfo[]> {
    // One request for the whole kind, properties included. Asking `Versioned` for the adapters and
    // then `GetStartupValues` per adapter was around ninety requests for the four kinds a
    // subscription screen loads, and each of those booted the adapter in a child process to be
    // told its property names.
    const rows = await get<RawCatalogAdapter[]>(`/adapters/Catalog?prefix=${KIND_PREFIX[kind]}`);
    return (rows ?? []).map((r) => ({
      id: r.key,
      kind,
      // No backend source for a friendly display name — fall back to the raw id.
      label: r.key,
      native: r.native,
      versions: r.versions ?? [],
      props: toProps(r.startupValues),
    }));
  },
} satisfies Partial<ApiClient>;
