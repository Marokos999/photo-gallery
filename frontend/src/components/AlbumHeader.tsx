"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { ShareButton } from "@/components/ShareButton";
import { api } from "@/lib/api";
import type { Album } from "@/lib/types";
import { button, input } from "@/lib/ui";

interface AlbumHeaderProps {
  album: Album;
  readyPhotoCount: number;
}

export function AlbumHeader({ album, readyPhotoCount }: AlbumHeaderProps) {
  const [editing, setEditing] = useState(false);
  const router = useRouter();
  const queryClient = useQueryClient();

  const rename = useMutation({
    mutationFn: (name: string) => api.renameAlbum(album.albumId, name),
    onSuccess: async () => {
      setEditing(false);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["album", album.albumId] }),
        queryClient.invalidateQueries({ queryKey: ["albums"] }),
      ]);
    },
  });

  const remove = useMutation({
    mutationFn: () => api.deleteAlbum(album.albumId),
    onSuccess: async () => {
      queryClient.removeQueries({ queryKey: ["album", album.albumId] });
      queryClient.removeQueries({ queryKey: ["photos", album.albumId] });
      await queryClient.invalidateQueries({ queryKey: ["albums"] });
      router.push("/");
    },
  });

  function submitRename(formData: FormData) {
    const name = String(formData.get("name") ?? "").trim();
    if (name && name !== album.name) rename.mutate(name);
    else setEditing(false);
  }

  function confirmDelete() {
    const photos = readyPhotoCount === 1 ? "1 photo" : `${readyPhotoCount} photos`;
    if (window.confirm(`Delete "${album.name}" and its ${photos}? This cannot be undone.`)) remove.mutate();
  }

  return (
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div className="min-w-0 flex-1">
        <Link href="/" className="text-sm text-neutral-500 hover:text-neutral-900">
          ← Albums
        </Link>

        {editing ? (
          <form action={submitRename} className="mt-1 flex max-w-md items-center gap-2">
            <input name="name" defaultValue={album.name} required maxLength={100} autoFocus className={input} />
            <button type="submit" disabled={rename.isPending} className={button.primary}>
              {rename.isPending ? "Saving…" : "Save"}
            </button>
            <button type="button" onClick={() => setEditing(false)} className={button.secondary}>
              Cancel
            </button>
          </form>
        ) : (
          <h1 className="truncate text-2xl font-semibold">{album.name}</h1>
        )}

        <p className="text-sm text-neutral-500">
          {readyPhotoCount} {readyPhotoCount === 1 ? "photo" : "photos"}
        </p>
        {rename.isError && <p className="text-sm text-red-600">Could not rename the album.</p>}
        {remove.isError && <p className="text-sm text-red-600">Could not delete the album.</p>}
      </div>

      <div className="flex flex-wrap items-start gap-2">
        {!editing && (
          <button type="button" onClick={() => setEditing(true)} className={button.secondary}>
            Rename
          </button>
        )}
        <button type="button" onClick={confirmDelete} disabled={remove.isPending} className={button.danger}>
          {remove.isPending ? "Deleting…" : "Delete album"}
        </button>
        <ShareButton albumId={album.albumId} />
      </div>
    </div>
  );
}
