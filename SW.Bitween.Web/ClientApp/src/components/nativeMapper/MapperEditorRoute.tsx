import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router";
import { api } from "../../api";
import { keys } from "../../api/queryKeys";
import { NATIVE_MAPPER_ID } from "../../lib/nativeMapper/types";
import { LoadingBlock } from "../ui/basics";
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

  const { data, isPending } = useQuery({
    queryKey: keys.subscriptions.detail(subscriptionId),
    queryFn: () => api.getSubscription(subscriptionId),
    enabled: Boolean(subscriptionId),
  });

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
