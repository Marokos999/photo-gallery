import Link from "next/link";
import type { Album } from "@/lib/types";

export function AlbumCard({album}: {album: Album}) {
  return(
    <Link href={`/album?id=${album.albumId}`} className="group block overflow-hidden rounded-xl border border-neutral-200 bg-white
      shadow-sm transition hover:shadow-md">
      <div className="aspect-4/3 overflow-hidden bg-neutral-100">
        {album.coverUrl ? (
          // eslint-disable-next-line @next/next/no-img-element -- static export: next/image optimization is unavailable
          <img
            src={album.coverUrl}
            alt=""
            loading="lazy"
            className="h-full w-full object-cover transition duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full items-center justify-center text-sm text-neutral-400">
            No photos yet
          </div>
        )}
      </div>
      <div className="p-3">
        <h2 className="truncate font-medium">{album.name}</h2>
        <p className="text-sm text-neutral-500">{album.photoCount} {album.photoCount === 1 ? "photo" : "photos"}</p>
      </div>
    </Link>
  );

}