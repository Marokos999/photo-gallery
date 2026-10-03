"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef } from "react";
import { api } from "@/lib/api";
import type { Photo, PhotoDetails } from "@/lib/types";
import { button, input, parseTags } from "@/lib/ui";

interface EditPhotoDialogProps {
  photo: Photo;
  onClose: () => void;
}

export function EditPhotoDialog({ photo, onClose }: EditPhotoDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const queryClient = useQueryClient();

  // Native <dialog>: focus trap, Esc to close and the backdrop come for free.
  useEffect(() => {
    dialogRef.current?.showModal();
  }, []);

  const update = useMutation({
    mutationFn: (details: PhotoDetails) => api.updatePhoto(photo.albumId, photo.photoId, details),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["photos", photo.albumId] }),
        queryClient.invalidateQueries({ queryKey: ["search"] }),
        queryClient.invalidateQueries({ queryKey: ["tags"] }),
      ]);
      onClose();
    },
  });

  function submit(formData: FormData) {
    const caption = String(formData.get("caption") ?? "").trim();
    update.mutate({ caption: caption || null, tags: parseTags(String(formData.get("tags") ?? "")) });
  }

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      className="m-auto w-full max-w-md rounded-xl bg-white p-0 shadow-xl backdrop:bg-black/50"
    >
      <form action={submit} className="space-y-4 p-6">
        <h2 className="text-lg font-semibold">Edit photo</h2>

        <label className="block space-y-1 text-sm">
          <span className="font-medium">Caption</span>
          <textarea name="caption" defaultValue={photo.caption ?? ""} maxLength={500} rows={3} className={input} />
        </label>

        <label className="block space-y-1 text-sm">
          <span className="font-medium">Tags</span>
          <input name="tags" defaultValue={photo.tags.join(", ")} placeholder="beach, summer" className={input} />
          <span className="text-xs text-neutral-500">Comma separated, up to 20 tags.</span>
        </label>

        {update.isError && <p className="text-sm text-red-600">Could not save the changes.</p>}

        <div className="flex justify-end gap-2">
          <button type="button" onClick={onClose} className={button.secondary}>
            Cancel
          </button>
          <button type="submit" disabled={update.isPending} className={button.primary}>
            {update.isPending ? "Saving…" : "Save"}
          </button>
        </div>
      </form>
    </dialog>
  );
}
