import { useCallback, useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../../api";
import { keys } from "../../api/queryKeys";
import { useRules, useRulesDispatch } from "../../lib/nativeMapper/RulesEditorContext";
import { loadMapping, saveMapping, toWire } from "../../lib/nativeMapper/serialize";
import { NATIVE_MAPPER_ID } from "../../lib/nativeMapper/types";

/** Loads a subscription's rules into the editor, and clears them when the id changes. */
export function useMappingLoader(subscriptionId: number) {
  const dispatch = useRulesDispatch();
  const { data } = useQuery({
    queryKey: keys.subscriptions.detail(subscriptionId),
    queryFn: () => api.getSubscription(subscriptionId),
    enabled: Boolean(subscriptionId),
  });

  // The subscription whose data we are waiting for. Without this, switching
  // subscriptions quickly can land the first one's rules in the second one's editor.
  const awaiting = useRef<number | null>(null);

  useEffect(() => {
    awaiting.current = subscriptionId || null;
  }, [subscriptionId]);

  useEffect(() => {
    if (!data || awaiting.current !== subscriptionId) return;
    const loaded = loadMapping(data.mapperProperties);
    dispatch({
      type: "LOAD",
      rules: loaded.rules,
      sourceSample: loaded.sourceSample,
      targetSample: loaded.targetSample,
      error: loaded.error,
    });
  }, [data, subscriptionId, dispatch]);

  return { partnerId: data?.partnerId ?? null };
}

const PREVIEW_DEBOUNCE_MS = 500;

/**
 * Keeps the preview in step with the rules.
 *
 * Posts the rules and the sample and shows what comes back, which is the same
 * read/map/write the exchange pipeline runs. Nothing is generated here and nothing
 * is evaluated in the browser, so what the editor shows is what a partner receives.
 */
export function useMappingPreview(partnerId: number | null) {
  const { rules, sourceSample } = useRules();
  const dispatch = useRulesDispatch();
  const [isPreviewing, setPreviewing] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const runId = useRef(0);

  useEffect(() => {
    if (timer.current) clearTimeout(timer.current);

    if (!sourceSample.trim()) {
      dispatch({ type: "PREVIEW_RESULT", output: null, ruleErrors: {}, error: null });
      return;
    }

    timer.current = setTimeout(async () => {
      const run = ++runId.current;
      setPreviewing(true);
      try {
        const result = await api.previewMappingRules({
          mappingRules: JSON.stringify(toWire(rules)),
          sourceDocument: sourceSample,
          partnerId,
        });

        // A slower earlier request must not overwrite a newer result.
        if (run !== runId.current) return;

        dispatch({
          type: "PREVIEW_RESULT",
          output: result.outputDocument,
          ruleErrors: Object.fromEntries(result.ruleErrors.map((e) => [e.target, e.reason])),
          error: result.error,
        });
      } catch {
        if (run !== runId.current) return;
        dispatch({
          type: "PREVIEW_RESULT",
          output: null,
          ruleErrors: {},
          error: "Could not reach the server to work out the preview.",
        });
      } finally {
        if (run === runId.current) setPreviewing(false);
      }
    }, PREVIEW_DEBOUNCE_MS);

    return () => {
      if (timer.current) clearTimeout(timer.current);
    };
  }, [rules, sourceSample, partnerId, dispatch]);

  return { isPreviewing };
}

/** Saves the rules onto the subscription, pointing it at this mapper. */
export function useMappingSave(subscriptionId: number) {
  const { rules, sourceSample, targetSample } = useRules();
  const dispatch = useRulesDispatch();
  const queryClient = useQueryClient();
  const [justSaved, setJustSaved] = useState(false);

  const mutation = useMutation({
    mutationFn: () =>
      api.updateSubscription(subscriptionId, {
        mapperId: NATIVE_MAPPER_ID,
        mapperProperties: saveMapping(rules, sourceSample, targetSample),
      }),
    onSuccess: () => {
      dispatch({ type: "SAVED" });
      setJustSaved(true);
      setTimeout(() => setJustSaved(false), 2000);
      return queryClient.invalidateQueries({
        queryKey: keys.subscriptions.detail(subscriptionId),
      });
    },
  });

  const save = useCallback(() => mutation.mutateAsync(), [mutation]);

  return {
    save,
    isSaving: mutation.isPending,
    justSaved,
    saveError: mutation.error ? (mutation.error as Error).message : null,
  };
}
