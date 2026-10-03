"use client";

import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { AlbumHeader } from "@/components/AlbumHeader";
import { LoadMoreSentinel } from "@/components/LoadMoreSentinel";
import { PhotoGrid } from "@/components/PhotoGrid";
import { UploadDropzone } from "@/components/UploadDropzone";
import { ApiError, api } from "@/lib/api";

const POLL_INTERVAL_MS = 3000;

export function AlbumView() {
  const albumId = useSearchParams().get("id") ?? "";

  const photos = useInfiniteQuery({
    queryKey: ["photos", albumId],
    queryFn: ({ pageParam }) => api.listPhotos(albumId, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    enabled: albumId !== "",
    // Poll while anything is still processing. Download URLs are stable server-side, so a poll that
    // changes nothing returns identical data and React re-renders nothing.
    refetchInterval: (query) =>
      query.state.data?.pages.some((page) => page.items.some((photo) => photo.status === "Pending"))
        ? POLL_INTERVAL_MS
        : false,
  });

  const allPhotos = photos.data?.pages.flatMap((page) => page.items) ?? [];
  const hasPending = allPhotos.some((photo) => photo.status === "Pending");

  const album = useQuery({
    queryKey: ["album", albumId],
    queryFn: () => api.getAlbum(albumId),
    enabled: albumId !== "",
    // The photo count only changes when processing finishes, so refresh it alongside the photos.
    refetchInterval: hasPending ? POLL_INTERVAL_MS : false,
  });

  const notFound = albumId === "" || (album.error instanceof ApiError && album.error.status === 404);

  if (notFound) {
    return (
      <p className="text-neutral-500">
        Album not found.{" "}
        <Link href="/" className="underline">
          Back to albums
        </Link>
      </p>
    );
  }

  if (album.isPending) return <p className="text-neutral-500">Loading album…</p>;
  if (album.isError) return <p className="text-red-600">Could not load the album.</p>;

  return (
    <section className="space-y-6">
      <AlbumHeader album={album.data} readyPhotoCount={album.data.photoCount} />
      <UploadDropzone albumId={albumId} />

      {photos.isPending && <p className="text-neutral-500">Loading photos…</p>}
      {photos.isError && <p className="text-red-600">Could not load photos.</p>}
      {photos.isSuccess && <PhotoGrid albumId={albumId} photos={allPhotos} />}

      {photos.hasNextPage && (
        <>
          <LoadMoreSentinel onVisible={() => void photos.fetchNextPage()} disabled={photos.isFetchingNextPage} />
          <p className="text-center text-sm text-neutral-500">
            {photos.isFetchingNextPage ? "Loading more photos…" : "Scroll for more"}
          </p>
        </>
      )}
    </section>
  );
}
