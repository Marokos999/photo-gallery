"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";

export function CreateAlbumForm() {
  const queryClient = useQueryClient();
  const createAlbum = useMutation({
    mutationFn: api.createAlbum,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["albums"] }),
  });

  function submit(formData: FormData) {
    const name = String(formData.get("name") ?? "").trim();
    if (name) createAlbum.mutate(name);
  }

  return (
    <form action={submit} className="flex flex-wrap items-center gap-2">
      <input
        name="name"
        required
        maxLength={100}
        placeholder="New album name"
        className="w-56 rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm focus:ring-2 focus:ring-neutral-400 focus:outline-none"
      />
      <button
        type="submit"
        disabled={createAlbum.isPending}
        className="rounded-lg bg-neutral-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-neutral-700 disabled:opacity-50"
      >
        {createAlbum.isPending ? "Creating…" : "Create album"}
      </button>
      {createAlbum.isError && <p className="w-full text-sm text-red-600">Could not create the album.</p>}
    </form>
  );
}
