"use client";

import { useQuery } from "@tanstack/react-query";
import { AlbumCard } from "@/components/AlbumCard";
import { CreateAlbumForm } from "@/components/CreateAlbumForm";
import { api } from "@/lib/api";

export default function AlbumsPage() {
  const { data: albums, isPending, isError } = useQuery({
    queryKey: ["albums"],
    queryFn: api.listAlbums,
    // Photo counts and covers change in the background (processing), so always refetch on visit.
    staleTime: 0,
  });

  return (
    <section className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold">Albums</h1>
        <CreateAlbumForm />
      </div>

      {isPending && <p className="text-neutral-500">Loading albums…</p>}
      {isError && <p className="text-red-600">Could not load albums. Is the API running?</p>}
      {albums?.length === 0 && <p className="text-neutral-500">No albums yet. Create your first one.</p>}

      <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
        {albums?.map((album) => (
          <li key={album.albumId}>
            <AlbumCard album={album} />
          </li>
        ))}
      </ul>
    </section>
  );
}