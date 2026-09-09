import type { ApiClient } from "../client";
import { post } from "./request";

interface RawMapperPreviewResponse {
  outputJson: string | null;
  error: string | null;
}

interface RawMappingPreviewResponse {
  outputDocument: string | null;
  contentType: string | null;
  ruleErrors: { target: string; reason: string }[] | null;
  error: string | null;
}

export const mapperMethods = {
  async previewMapping(input: {
    scribanTemplate: string;
    inputJson: string;
    partnerId?: number | null;
  }): Promise<{ outputJson: string | null; error: string | null }> {
    const res = await post<RawMapperPreviewResponse>("/mappers", input);
    return { outputJson: res.outputJson ?? null, error: res.error ?? null };
  },

  async previewMappingRules(input: {
    mappingRules: string;
    sourceDocument: string;
    partnerId?: number | null;
  }): Promise<{
    outputDocument: string | null;
    contentType: string | null;
    ruleErrors: { target: string; reason: string }[];
    error: string | null;
  }> {
    const res = await post<RawMappingPreviewResponse>("/mappingpreviews", input);
    return {
      outputDocument: res.outputDocument ?? null,
      contentType: res.contentType ?? null,
      ruleErrors: res.ruleErrors ?? [],
      error: res.error ?? null,
    };
  },
} satisfies Partial<ApiClient>;
