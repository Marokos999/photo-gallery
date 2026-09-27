"use client";

import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "@/lib/api";

const SHARE_DAYS = 7;

export function ShareButton({ albumId }: { albumId: string }) {
  const [copied, setCopied] = useState(false);
  const share = useMutation({ mutationFn: () => api.createShare(albumId, SHARE_DAYS) });

  const link = share.data ? `${window.location.origin}/shared?code=${share.data.code}` : null;

  async function copyLink() {
    if (!link) return;
    await navigator.clipboard.writeText(link);
    setCopied(true);
  }

  return (
    <div className="flex flex-col items-end gap-2">
      <button
        type="button"
        onClick={() => share.mutate()}
        disabled={share.isPending}
        className="rounded-lg border border-neutral-300 bg-white px-4 py-2 text-sm font-medium transition hover:bg-neutral-100 disabled:opacity-50"
      >
        {share.isPending ? "Creating link…" : "Share album"}
      </button>

      {link && (
        <div className="flex items-center gap-2">
          <input
            readOnly
            value={link}
            onFocus={(event) => event.currentTarget.select()}
            className="w-72 rounded-md border border-neutral-300 bg-white px-2 py-1 text-xs"
          />
          <button type="button" onClick={copyLink} className="text-sm font-medium underline">
            {copied ? "Copied" : "Copy"}
          </button>
        </div>
      )}

      {share.data && (
        <p className="text-xs text-neutral-500">Expires {new Date(share.data.expiresAt).toLocaleDateString()}</p>
      )}
      {share.isError && <p className="text-sm text-red-600">Could not create a share link.</p>}
    </div>
  );
}
