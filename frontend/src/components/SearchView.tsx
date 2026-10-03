"use client";

import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { LoadMoreSentinel } from "@/components/LoadMoreSentinel";
import { PhotoGrid } from "@/components/PhotoGrid";
import { api } from "@/lib/api";
import { button, input } from "@/lib/ui";

export function SearchView() {
  const tag = useSearchParams().get("tag")?.trim() ?? "";
  const router = useRouter();

  const tags = useQuery({ queryKey: ["tags"], queryFn: api.listTags });

  const results = useInfiniteQuery({
    queryKey: ["search", tag.toLowerCase()],
    queryFn: ({ pageParam }) => api.searchPhotos(tag, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    enabled: tag !== "",
  });

  const photos = results.data?.pages.flatMap((page) => page.items) ?? [];

  function submit(formData: FormData) {
    const next = String(formData.get("tag") ?? "")
      .trim()
      .replace(/^#/, "");
    if (next) router.push(`/search?tag=${encodeURIComponent(next)}`);
  }

  return (
    <section className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <h1 className="text-2xl font-semibold">{tag ? `#${tag}` : "Search by tag"}</h1>
        <form action={submit} className="flex gap-2" key={tag}>
          <input name="tag" defaultValue={tag} placeholder="beach" maxLength={50} className={`${input} w-56`} />
          <button type="submit" className={button.primary}>
            Search
          </button>
        </form>
      </div>

      {tags.data && tags.data.length > 0 && (
        <ul className="flex flex-wrap gap-2" aria-label="Your tags">
          {tags.data.map(({ tag: name, count }) => {
            const active = name.toLowerCase() === tag.toLowerCase();
            return (
              <li key={name}>
                <Link
                  href={`/search?tag=${encodeURIComponent(name)}`}
                  aria-current={active ? "page" : undefined}
                  className={`inline-flex items-center gap-1 rounded-full border px-3 py-1 text-sm transition ${
                    active
                      ? "border-neutral-900 bg-neutral-900 text-white"
                      : "border-neutral-300 bg-white hover:border-neutral-400"
                  }`}
                >
                  #{name}
                  <span className={active ? "text-neutral-300" : "text-neutral-400"}>{count}</span>
                </Link>
              </li>
            );
          })}
        </ul>
      )}
      {tags.data?.length === 0 && (
        <p className="text-neutral-500">No tags yet. Add tags when uploading or by editing a photo.</p>
      )}

      {tag && results.isPending && <p className="text-neutral-500">Searching…</p>}
      {results.isError && <p className="text-red-600">Search failed.</p>}
      {results.isSuccess && <PhotoGrid photos={photos} emptyMessage={`No photos tagged #${tag}.`} />}

      {results.hasNextPage && (
        <LoadMoreSentinel onVisible={() => void results.fetchNextPage()} disabled={results.isFetchingNextPage} />
      )}
    </section>
  );
}
