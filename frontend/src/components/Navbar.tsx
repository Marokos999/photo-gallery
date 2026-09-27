"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { config } from "@/lib/config";

const links = [
  { href: "/", label: "Albums", matches: (path: string) => path === "/" || path.startsWith("/album") },
  { href: "/upload", label: "Upload", matches: (path: string) => path.startsWith("/upload") },
];

export function Navbar() {
  const pathname = usePathname();
  const isPublicPage = pathname.startsWith("/shared");

  return (
    <header className="sticky top-0 z-10 border-b border-neutral-200 bg-white/90 backdrop-blur">
      <nav className="mx-auto flex max-w-6xl items-center gap-6 px-4 py-3">
        {isPublicPage ? (
          <span className="text-lg font-semibold tracking-tight">Photo Gallery</span>
        ) : (
          <Link href="/" className="text-lg font-semibold tracking-tight">
            Photo Gallery
          </Link>
        )}

        {!isPublicPage && (
          <>
            <ul className="flex items-center gap-1 text-sm">
              {links.map((link) => {
                const active = link.matches(pathname);
                return (
                  <li key={link.href}>
                    <Link
                      href={link.href}
                      aria-current={active ? "page" : undefined}
                      className={`rounded-md px-3 py-1.5 transition ${
                        active ? "bg-neutral-900 text-white" : "text-neutral-600 hover:bg-neutral-100 hover:text-neutral-900"
                      }`}
                    >
                      {link.label}
                    </Link>
                  </li>
                );
              })}
            </ul>

            {config.debugUserId && (
              <span
                title="Local development user (x-debug-user-id)"
                className="ml-auto rounded-full bg-amber-100 px-3 py-1 text-xs font-medium text-amber-800"
              >
                {config.debugUserId} · dev
              </span>
            )}
          </>
        )}
      </nav>
    </header>
  );
}
