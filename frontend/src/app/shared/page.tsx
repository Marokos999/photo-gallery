import { Suspense } from "react";
import { SharedAlbumView } from "@/components/SharedAlbumView";

export default function SharedAlbumPage() {
  return (
    <Suspense fallback={<p className="text-neutral-500">Loading album…</p>}>
      <SharedAlbumView />
    </Suspense>
  );
}
