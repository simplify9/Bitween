import { useQuery } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router";
import { api } from "../../api";
import { keys } from "../../api/queryKeys";
import { NATIVE_MAPPER_ID } from "../../lib/nativeMapper/types";
import { Button, FormError, LoadingBlock } from "../ui/basics";
import MappingEditor from "../mapper/MappingEditor";
import NativeMapperEditor from "./NativeMapperEditor";

/**
 * Sends a subscription to the editor that matches its mapper.
 *
 * Two editors coexist on purpose. The old one speaks the old mapper's model — six
 * optional fields per mapping, a flat list of arrays joined by parent ids, dotted
 * target strings — and those are the three things the new model replaced, so there
 * is no useful middle ground between them. When the old mapper is retired its
 * editor is deleted rather than untangled.
 */
export default function MapperEditorRoute() {
  const { id } = useParams<{ id: string }>();
  const subscriptionId = Number(id);

  const valid = Number.isInteger(subscriptionId) && subscriptionId > 0;

  const { data, isPending, error } = useQuery({
    queryKey: keys.subscriptions.detail(subscriptionId),
    queryFn: () => api.getSubscription(subscriptionId),
    enabled: valid,
  });

  // A disabled query stays pending for ever, so an unreadable id in the URL would
  // sit under "Opening the mapping…" with nothing ever arriving.
  if (!valid) return <Problem message="That is not a subscription this editor can open." />;

  // The subscription could not be read, so which mapper it uses is unknown. Guessing
  // would open an editor showing no rules, and saving from there would replace a real
  // mapping with an empty one.
  if (error)
    return (
      <Problem
        message={`This subscription's mapping could not be loaded: ${(error as Error).message}`}
      />
    );

  // Waiting rather than guessing: opening the wrong editor would show a mapping
  // that looks empty, and saving from there would overwrite the real one.
  if (isPending) {
    return (
      <div className="fixed inset-0 z-40 flex items-center justify-center bg-white">
        <LoadingBlock label="Opening the mapping…" />
      </div>
    );
  }

  // A subscription with no mapper yet gets the new editor — that is what new
  // mappings should be built with. Existing ones keep whichever they were made in.
  const useNew = !data?.mapperId || data.mapperId === NATIVE_MAPPER_ID;

  return useNew ? <NativeMapperEditor /> : <MappingEditor />;
}

/** Says why the editor will not open, rather than opening it on a guess. */
function Problem({ message }: { message: string }) {
  const navigate = useNavigate();

  return (
    <div className="fixed inset-0 z-40 flex flex-col items-center justify-center gap-4 bg-white px-6">
      <FormError>{message}</FormError>
      <Button onClick={() => navigate("/subscriptions")}>Back to subscriptions</Button>
    </div>
  );
}
