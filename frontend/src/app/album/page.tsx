import { Suspense } from "react";
import { AlbumView } from "@/components/AlbumView";

export default function AlbumPage() {
  return (
    <Suspense fallback={<p className="text-neutral-500">Loading album…</p>}>
      <AlbumView />
    </Suspense>
  );
}
