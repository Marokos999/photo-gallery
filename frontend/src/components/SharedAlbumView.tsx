"use client";

import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import Lightbox from "yet-another-react-lightbox";
import "yet-another-react-lightbox/styles.css";
import { ApiError, api } from "@/lib/api";
import { tileAspectRatio } from "@/lib/ui";

export function SharedAlbumView() {
  const code = useSearchParams().get("code") ?? "";
  const [lightboxIndex, setLightboxIndex] = useState(-1);

  const shared = useQuery({
    queryKey: ["shared", code],
    queryFn: () => api.getSharedAlbum(code),
    enabled: code !== "",
  });

  if (code === "" || (shared.error instanceof ApiError && shared.error.status === 404)) {
    return (
      <div className="mx-auto max-w-md py-16 text-center">
        <h1 className="text-xl font-semibold">Link not available</h1>
        <p className="mt-2 text-neutral-500">This share link is invalid or has expired.</p>
      </div>
    );
  }

  if (shared.isPending) return <p className="text-neutral-500">Loading album…</p>;
  if (shared.isError) return <p className="text-red-600">Could not load the shared album.</p>;

  const { name, expiresAt, photos } = shared.data;
  const slides = photos
    .filter((photo) => photo.previewUrl)
    .map((photo) => ({ src: photo.previewUrl!, alt: photo.caption ?? "" }));

  return (
    <section className="space-y-6">
      <div>
        <p className="text-sm text-neutral-500">Shared album</p>
        <h1 className="text-2xl font-semibold">{name}</h1>
        {expiresAt && (
          <p className="text-sm text-neutral-500">Link expires {new Date(expiresAt).toLocaleDateString()}</p>
        )}
      </div>

      {photos.length === 0 ? (
        <p className="text-neutral-500">This album has no photos yet.</p>
      ) : (
        <ul className="columns-2 gap-3 sm:columns-3 lg:columns-4">
          {photos.map((photo, index) => (
            <li key={photo.photoId} className="mb-3 break-inside-avoid">
              <button
                type="button"
                onClick={() => setLightboxIndex(index)}
                className="block w-full cursor-zoom-in overflow-hidden rounded-lg bg-neutral-200"
                style={{ aspectRatio: tileAspectRatio(photo.width, photo.height) }}
              >
                {/* eslint-disable-next-line @next/next/no-img-element -- static export: next/image optimization is unavailable */}
                <img
                  src={photo.thumbnailUrl ?? ""}
                  alt={photo.caption ?? ""}
                  loading="lazy"
                  className="h-full w-full object-cover"
                />
              </button>
              {photo.caption && <p className="mt-1 text-sm text-neutral-600">{photo.caption}</p>}
            </li>
          ))}
        </ul>
      )}

      <Lightbox open={lightboxIndex >= 0} index={lightboxIndex} close={() => setLightboxIndex(-1)} slides={slides} />
    </section>
  );
}
