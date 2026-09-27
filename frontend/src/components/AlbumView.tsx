"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { AlbumHeader } from "@/components/AlbumHeader";
import { PhotoGrid } from "@/components/PhotoGrid";
import { UploadDropzone } from "@/components/UploadDropzone";
import { ApiError, api } from "@/lib/api";

const POLL_INTERVAL_MS = 3000;

export function AlbumView() {
  const albumId = useSearchParams().get("id") ?? "";

  const album = useQuery({
    queryKey: ["album", albumId],
    queryFn: () => api.getAlbum(albumId),
    enabled: albumId !== "",
  });

  const photos = useQuery({
    queryKey: ["photos", albumId],
    queryFn: () => api.listPhotos(albumId),
    enabled: albumId !== "",
    refetchInterval: (query) =>
      query.state.data?.some((photo) => photo.status === "Pending") ? POLL_INTERVAL_MS : false,
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

  // Derived from the (polled) photo list so the count updates as soon as processing finishes.
  const readyPhotoCount =
    photos.data?.filter((photo) => photo.status === "Ready").length ?? album.data.photoCount;

  return (
    <section className="space-y-6">
      <AlbumHeader album={album.data} readyPhotoCount={readyPhotoCount} />
      <UploadDropzone albumId={albumId} />

      {photos.isPending && <p className="text-neutral-500">Loading photos…</p>}
      {photos.isError && <p className="text-red-600">Could not load photos.</p>}
      {photos.data && <PhotoGrid albumId={albumId} photos={photos.data} />}
    </section>
  );
}
