import { Suspense } from "react";
import { SearchView } from "@/components/SearchView";

export default function SearchPage() {
  return (
    <Suspense fallback={<p className="text-neutral-500">Loading…</p>}>
      <SearchView />
    </Suspense>
  );
}
