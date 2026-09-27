"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import Lightbox from "yet-another-react-lightbox";
import "yet-another-react-lightbox/styles.css";
import { EditPhotoDialog } from "@/components/EditPhotoDialog";
import { api } from "@/lib/api";
import type { Photo } from "@/lib/types";
import { tileAspectRatio } from "@/lib/ui";

interface PhotoGridProps {
  albumId: string;
  photos: Photo[];
}

export function PhotoGrid({ albumId, photos }: PhotoGridProps) {
  const [lightboxIndex, setLightboxIndex] = useState(-1);
  const [editing, setEditing] = useState<Photo | null>(null);
  const queryClient = useQueryClient();

  const deletePhoto = useMutation({
    mutationFn: (photoId: string) => api.deletePhoto(albumId, photoId),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["photos", albumId] }),
        queryClient.invalidateQueries({ queryKey: ["album", albumId] }),
        queryClient.invalidateQueries({ queryKey: ["albums"] }),
      ]),
  });

  const readyPhotos = photos.filter((photo) => photo.status === "Ready" && photo.previewUrl);
  const slides = readyPhotos.map((photo) => ({ src: photo.previewUrl!, alt: photo.caption ?? "" }));

  function confirmDelete(photo: Photo) {
    if (window.confirm("Delete this photo? This cannot be undone.")) deletePhoto.mutate(photo.photoId);
  }

  if (photos.length === 0) {
    return <p className="text-neutral-500">No photos in this album yet.</p>;
  }

  return (
    <>
      <ul className="columns-2 gap-3 sm:columns-3 lg:columns-4">
        {photos.map((photo) => (
          <li key={photo.photoId} className="mb-3 break-inside-avoid">
            <PhotoTile
              photo={photo}
              deleting={deletePhoto.isPending && deletePhoto.variables === photo.photoId}
              onOpen={() => setLightboxIndex(readyPhotos.indexOf(photo))}
              onEdit={() => setEditing(photo)}
              onDelete={() => confirmDelete(photo)}
            />
          </li>
        ))}
      </ul>

      <Lightbox open={lightboxIndex >= 0} index={lightboxIndex} close={() => setLightboxIndex(-1)} slides={slides} />

      {editing && <EditPhotoDialog albumId={albumId} photo={editing} onClose={() => setEditing(null)} />}
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
  const aspectRatio = tileAspectRatio(photo.width, photo.height);
  const isReady = photo.status === "Ready" && photo.thumbnailUrl;

  return (
    <div className="group relative overflow-hidden rounded-lg bg-neutral-200" style={{ aspectRatio }}>
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
                <li key={tag} className="rounded bg-white/20 px-1.5 py-0.5 text-[11px]">
                  #{tag}
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
