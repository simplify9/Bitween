import { useMemo } from "react";
import { useNavigate, useParams } from "react-router";
import { ArrowLeft, Check, Redo2, Undo2 } from "lucide-react";
import {
  RulesEditorProvider,
  useRules,
  useRulesDispatch,
} from "../../lib/nativeMapper/RulesEditorContext";
import { listPaths, parseSample, readablePaths } from "../../lib/nativeMapper/documentTree";
import { everyFieldRule } from "../../lib/nativeMapper/rulesReducer";
import { DOCUMENT_FORMATS, type DocumentFormatId } from "../../lib/nativeMapper/types";
import { Button, FormError } from "../ui/basics";
import { Select } from "../ui/forms";
import { OutputPanel } from "./OutputPanel";
import { PreviewPanel } from "./PreviewPanel";
import { SourcePanel } from "./SourcePanel";
import { useMappingLoader, useMappingPreview, useMappingSave } from "./useMapping";

export default function NativeMapperEditor() {
  return (
    <RulesEditorProvider>
      <Editor />
    </RulesEditorProvider>
  );
}

function Editor() {
  const { id } = useParams<{ id: string }>();
  const subscriptionId = Number(id);
  const navigate = useNavigate();

  const { rules, sourceSample, selectedId, loadError, dirty } = useRules();
  const dispatch = useRulesDispatch();

  const { partnerId } = useMappingLoader(subscriptionId);
  const { isPreviewing } = useMappingPreview(partnerId);
  const { save, isSaving, justSaved, saveError } = useMappingSave(subscriptionId);

  const sample = useMemo(
    () => parseSample(sourceSample, rules.sourceFormat),
    [sourceSample, rules.sourceFormat],
  );
  const sourcePaths = useMemo(() => readablePaths(sample.root), [sample.root]);
  const listPathOptions = useMemo(() => listPaths(sample.root), [sample.root]);

  /** Which source paths a rule already reads, for the dot beside each field. */
  const assignedPaths = useMemo(() => {
    const paths = new Set<string>();
    for (const { rule } of everyFieldRule(rules))
      if (rule.from.kind === "path" && rule.from.path) paths.add(rule.from.path);
    return paths;
  }, [rules]);

  /** Clicking a source field fills whichever rule is selected. */
  const pickSourcePath = (path: string) => {
    if (!selectedId) return;
    dispatch({ type: "UPDATE_FIELD", id: selectedId, changes: { from: { kind: "path", path } } });
  };

  if (loadError) {
    return (
      <div className="fixed inset-0 z-40 flex flex-col items-center justify-center gap-4 bg-white px-6">
        <FormError>{loadError}</FormError>
        <p className="max-w-prose text-center text-sm text-ink-600">
          Nothing has been changed. Saving from here would replace the stored rules, so the editor
          will not open them.
        </p>
        <Button onClick={() => navigate(`/subscriptions/${subscriptionId}`)}>
          Back to the subscription
        </Button>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 z-40 flex flex-col overflow-hidden bg-white">
      {/* ── Toolbar ───────────────────────────────────────────────────────────── */}
      <div className="flex flex-shrink-0 flex-wrap items-center gap-2 border-b border-ink-200 px-3 py-2">
        <button
          type="button"
          onClick={() => navigate(`/subscriptions/${subscriptionId}`)}
          className="flex items-center gap-1 rounded px-1.5 py-1 text-sm text-ink-600 hover:bg-ink-50"
        >
          <ArrowLeft size={14} /> Back
        </button>

        <span className="text-sm font-semibold text-ink-900">Mapping</span>

        <div className="ml-2 flex items-center gap-1.5">
          <FormatSelect
            label="From"
            value={rules.sourceFormat}
            onChange={(format) => dispatch({ type: "SET_SOURCE_FORMAT", format })}
          />
          <span aria-hidden className="text-ink-300">
            →
          </span>
          <FormatSelect
            label="To"
            value={rules.targetFormat}
            onChange={(format) => dispatch({ type: "SET_TARGET_FORMAT", format })}
          />
        </div>

        <div className="ml-auto flex items-center gap-1.5">
          <button
            type="button"
            onClick={() => dispatch({ type: "UNDO" })}
            title="Undo"
            aria-label="Undo"
            className="rounded p-1.5 text-ink-500 hover:bg-ink-50"
          >
            <Undo2 size={14} />
          </button>
          <button
            type="button"
            onClick={() => dispatch({ type: "REDO" })}
            title="Redo"
            aria-label="Redo"
            className="rounded p-1.5 text-ink-500 hover:bg-ink-50"
          >
            <Redo2 size={14} />
          </button>

          {justSaved ? (
            <span className="flex items-center gap-1 text-sm font-medium text-ok-600">
              <Check size={14} /> Saved
            </span>
          ) : (
            <Button variant="primary" busy={isSaving} disabled={!dirty} onClick={() => void save()}>
              Save
            </Button>
          )}
        </div>
      </div>

      {saveError && (
        <div className="flex-shrink-0 border-b border-danger-100 bg-danger-50 px-3 py-2">
          <p className="text-xs text-danger-700">{saveError}</p>
        </div>
      )}

      {/* ── Panels ────────────────────────────────────────────────────────────── */}
      <div className="flex flex-1 overflow-hidden">
        <SourcePanel
          root={sample.root}
          parseError={sample.error}
          assignedPaths={assignedPaths}
          onPick={pickSourcePath}
        />

        <div className="flex flex-1 divide-x divide-ink-200 overflow-hidden">
          <OutputPanel listPathOptions={listPathOptions} />
          <div className="w-[38%] min-w-[280px] overflow-hidden">
            <PreviewPanel isPreviewing={isPreviewing} />
          </div>
        </div>
      </div>

      {/* A rule has to be selected for a source click to land somewhere, so say so
          rather than letting the click appear to do nothing. */}
      <div className="flex-shrink-0 border-t border-ink-200 bg-ink-50 px-3 py-1.5">
        <p className="text-[11px] text-ink-500">
          {selectedId
            ? "Click a source field on the left to use it in the selected rule."
            : sourcePaths.length > 0
              ? "Select a rule on the right, then click a source field to fill it."
              : "Paste a sample document to see the fields you can map."}
        </p>
      </div>
    </div>
  );
}

function FormatSelect({
  label,
  value,
  onChange,
}: {
  label: string;
  value: DocumentFormatId;
  onChange: (format: DocumentFormatId) => void;
}) {
  return (
    <label className="flex items-center gap-1 text-[11px] text-ink-500">
      {label}
      <Select
        className="h-8 w-24 text-xs"
        aria-label={`${label} format`}
        value={value}
        onChange={(e) => onChange(e.target.value as DocumentFormatId)}
        options={DOCUMENT_FORMATS.map((f) => ({ value: f.id, label: f.label }))}
      />
    </label>
  );
}
