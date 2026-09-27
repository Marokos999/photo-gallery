"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { UploadDropzone } from "@/components/UploadDropzone";
import { api } from "@/lib/api";
import { input, parseTags } from "@/lib/ui";

export default function UploadPage() {
  const albums = useQuery({ queryKey: ["albums"], queryFn: api.listAlbums });
  const [selectedAlbumId, setSelectedAlbumId] = useState("");
  const [caption, setCaption] = useState("");
  const [tagsText, setTagsText] = useState("");

  const albumId = selectedAlbumId || albums.data?.[0]?.albumId || "";

  return (
    <section className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Upload photos</h1>
        <p className="text-sm text-neutral-500">Caption and tags apply to every photo in this batch. You can edit them later.</p>
      </div>

      {albums.isPending && <p className="text-neutral-500">Loading albums…</p>}
      {albums.isError && <p className="text-red-600">Could not load albums.</p>}

      {albums.data?.length === 0 && (
        <p className="text-neutral-500">
          You need an album first.{" "}
          <Link href="/" className="underline">
            Create one
          </Link>
        </p>
      )}

      {albums.data && albums.data.length > 0 && (
        <>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="block space-y-1 text-sm">
              <span className="font-medium">Album</span>
              <select value={albumId} onChange={(event) => setSelectedAlbumId(event.target.value)} className={input}>
                {albums.data.map((album) => (
                  <option key={album.albumId} value={album.albumId}>
                    {album.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="block space-y-1 text-sm">
              <span className="font-medium">Tags</span>
              <input
                value={tagsText}
                onChange={(event) => setTagsText(event.target.value)}
                placeholder="beach, summer"
                className={input}
              />
            </label>

            <label className="block space-y-1 text-sm sm:col-span-2">
              <span className="font-medium">Caption</span>
              <input
                value={caption}
                onChange={(event) => setCaption(event.target.value)}
                maxLength={500}
                placeholder="Optional"
                className={input}
              />
            </label>
          </div>

          <UploadDropzone albumId={albumId} caption={caption.trim() || undefined} tags={parseTags(tagsText)} />

          <Link href={`/album?id=${albumId}`} className="inline-block text-sm font-medium underline">
            Open album →
          </Link>
        </>
      )}
    </section>
  );
}
