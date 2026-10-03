"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import Lightbox from "yet-another-react-lightbox";
import "yet-another-react-lightbox/styles.css";
import { useConfirm } from "@/components/ConfirmDialog";
import { EditPhotoDialog } from "@/components/EditPhotoDialog";
import { api } from "@/lib/api";
import type { Photo } from "@/lib/types";
import { tileAspectRatio } from "@/lib/ui";

interface PhotoGridProps {
  photos: Photo[];
  emptyMessage?: string;
}

/** Owner's grid: works for one album or for search results spanning albums (each photo knows its album). */
export function PhotoGrid({ photos, emptyMessage = "No photos in this album yet." }: PhotoGridProps) {
  const [lightboxIndex, setLightboxIndex] = useState(-1);
  const [editing, setEditing] = useState<Photo | null>(null);
  const [confirm, confirmDialog] = useConfirm();
  const queryClient = useQueryClient();

  const deletePhoto = useMutation({
    mutationFn: (photo: Photo) => api.deletePhoto(photo.albumId, photo.photoId),
    onSuccess: (_, photo) =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["photos", photo.albumId] }),
        queryClient.invalidateQueries({ queryKey: ["album", photo.albumId] }),
        queryClient.invalidateQueries({ queryKey: ["albums"] }),
        queryClient.invalidateQueries({ queryKey: ["search"] }),
        queryClient.invalidateQueries({ queryKey: ["tags"] }),
      ]),
  });

  const readyPhotos = photos.filter((photo) => photo.status === "Ready" && photo.previewUrl);
  const slides = readyPhotos.map((photo) => ({ src: photo.previewUrl!, alt: photo.caption ?? "" }));

  async function confirmDelete(photo: Photo) {
    const confirmed = await confirm({
      title: "Delete photo?",
      message: "The photo and its thumbnails are removed permanently.",
    });
    if (confirmed) deletePhoto.mutate(photo);
  }

  if (photos.length === 0) {
    return <p className="text-neutral-500">{emptyMessage}</p>;
  }

  return (
    <>
      <ul className="columns-2 gap-3 sm:columns-3 lg:columns-4">
        {photos.map((photo) => (
          <li key={photo.photoId} className="mb-3 break-inside-avoid">
            <PhotoTile
              photo={photo}
              deleting={deletePhoto.isPending && deletePhoto.variables?.photoId === photo.photoId}
              onOpen={() => setLightboxIndex(readyPhotos.indexOf(photo))}
              onEdit={() => setEditing(photo)}
              onDelete={() => void confirmDelete(photo)}
            />
          </li>
        ))}
      </ul>

      <Lightbox open={lightboxIndex >= 0} index={lightboxIndex} close={() => setLightboxIndex(-1)} slides={slides} />

      {editing && <EditPhotoDialog photo={editing} onClose={() => setEditing(null)} />}
      {confirmDialog}
    </>
  );
}

interface PhotoTileProps {
  photo: Photo;
  deleting: boolean;
  onOpen: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

const tileAction =
  "rounded-md bg-black/60 px-2 py-1 text-xs text-white opacity-0 transition group-hover:opacity-100 focus:opacity-100 disabled:opacity-50";

function PhotoTile({ photo, deleting, onOpen, onEdit, onDelete }: PhotoTileProps) {
  const isReady = photo.status === "Ready" && photo.thumbnailUrl;

  return (
    <div
      className="group relative overflow-hidden rounded-lg bg-neutral-200"
      style={{ aspectRatio: tileAspectRatio(photo.width, photo.height) }}
    >
      {isReady ? (
        <button type="button" onClick={onOpen} className="block h-full w-full cursor-zoom-in">
          {/* eslint-disable-next-line @next/next/no-img-element -- static export: next/image optimization is unavailable */}
          <img
            src={photo.thumbnailUrl!}
            alt={photo.caption ?? ""}
            loading="lazy"
            className="h-full w-full object-cover"
          />
        </button>
      ) : (
        <div className="flex h-full items-center justify-center text-sm text-neutral-500">
          {photo.status === "Pending" ? "Processing…" : "Processing failed"}
        </div>
      )}

      {(photo.caption || photo.tags.length > 0) && (
        <div className="pointer-events-none absolute inset-x-0 bottom-0 space-y-1 bg-linear-to-t from-black/70 to-transparent p-2 text-white">
          {photo.caption && <p className="text-sm">{photo.caption}</p>}
          {photo.tags.length > 0 && (
            <ul className="flex flex-wrap gap-1">
              {photo.tags.map((tag) => (
                <li key={tag}>
                  <Link
                    href={`/search?tag=${encodeURIComponent(tag)}`}
                    className="pointer-events-auto rounded bg-white/20 px-1.5 py-0.5 text-[11px] transition hover:bg-white/40"
                  >
                    #{tag}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      <div className="absolute top-2 right-2 flex gap-1">
        <button type="button" onClick={onEdit} aria-label="Edit photo" className={tileAction}>
          Edit
        </button>
        <button type="button" onClick={onDelete} disabled={deleting} aria-label="Delete photo" className={tileAction}>
          {deleting ? "Deleting…" : "Delete"}
        </button>
      </div>
    </div>
  );
}
