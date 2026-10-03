"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef, useState } from "react";
import { useConfirm } from "@/components/ConfirmDialog";
import { api } from "@/lib/api";
import type { ShareSummary } from "@/lib/types";
import { button, input } from "@/lib/ui";

const EXPIRY_OPTIONS = [1, 7, 30] as const;

export function ShareManager({ albumId }: { albumId: string }) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button type="button" onClick={() => setOpen(true)} className={button.secondary}>
        Share album
      </button>
      {open && <ShareDialog albumId={albumId} onClose={() => setOpen(false)} />}
    </>
  );
}

function ShareDialog({ albumId, onClose }: { albumId: string; onClose: () => void }) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [days, setDays] = useState<number>(7);
  const [copied, setCopied] = useState<string | null>(null);
  const [confirm, confirmDialog] = useConfirm();
  const queryClient = useQueryClient();
  const sharesKey = ["shares", albumId];

  useEffect(() => {
    dialogRef.current?.showModal();
  }, []);

  const shares = useQuery({ queryKey: sharesKey, queryFn: () => api.listShares(albumId) });

  const create = useMutation({
    mutationFn: () => api.createShare(albumId, days),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: sharesKey }),
  });

  const revoke = useMutation({
    mutationFn: (code: string) => api.revokeShare(code),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: sharesKey }),
  });

  const linkFor = (code: string) => `${window.location.origin}/shared?code=${code}`;

  async function copy(code: string) {
    await navigator.clipboard.writeText(linkFor(code));
    setCopied(code);
  }

  async function confirmRevoke(share: ShareSummary) {
    const confirmed = await confirm({
      title: "Revoke link?",
      message: "Anyone using this link will no longer be able to open the album.",
      confirmLabel: "Revoke",
    });
    if (confirmed) revoke.mutate(share.code);
  }

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      aria-labelledby="share-title"
      className="m-auto w-full max-w-lg rounded-xl bg-white p-0 shadow-xl backdrop:bg-black/50"
    >
      <div className="space-y-5 p-6">
        <div className="flex items-start justify-between gap-4">
          <h2 id="share-title" className="text-lg font-semibold">
            Share album
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="text-neutral-500 hover:text-neutral-900"
          >
            ✕
          </button>
        </div>

        <div className="flex flex-wrap items-end gap-2">
          <label className="block space-y-1 text-sm">
            <span className="font-medium">Link expires after</span>
            <select value={days} onChange={(event) => setDays(Number(event.target.value))} className={input}>
              {EXPIRY_OPTIONS.map((option) => (
                <option key={option} value={option}>
                  {option === 1 ? "1 day" : `${option} days`}
                </option>
              ))}
            </select>
          </label>
          <button type="button" onClick={() => create.mutate()} disabled={create.isPending} className={button.primary}>
            {create.isPending ? "Creating…" : "Create link"}
          </button>
        </div>
        {create.isError && <p className="text-sm text-red-600">Could not create a share link.</p>}

        <section className="space-y-2">
          <h3 className="text-sm font-medium text-neutral-700">Links</h3>
          {shares.isPending && <p className="text-sm text-neutral-500">Loading…</p>}
          {shares.data?.length === 0 && <p className="text-sm text-neutral-500">No links yet.</p>}
          <ul className="space-y-2">
            {shares.data?.map((share) => (
              <li
                key={share.code}
                className="flex flex-wrap items-center gap-2 rounded-lg border border-neutral-200 px-3 py-2 text-sm"
              >
                <span className={`flex-1 truncate font-mono text-xs ${share.isExpired ? "text-neutral-400" : ""}`}>
                  {linkFor(share.code)}
                </span>
                <span className="text-xs text-neutral-500">
                  {share.isExpired
                    ? "Expired"
                    : share.expiresAt
                      ? `Until ${new Date(share.expiresAt).toLocaleDateString()}`
                      : "No expiry"}
                </span>
                {!share.isExpired && (
                  <button type="button" onClick={() => void copy(share.code)} className="text-xs font-medium underline">
                    {copied === share.code ? "Copied" : "Copy"}
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => void confirmRevoke(share)}
                  disabled={revoke.isPending && revoke.variables === share.code}
                  className="text-xs font-medium text-red-700 underline disabled:opacity-50"
                >
                  Revoke
                </button>
              </li>
            ))}
          </ul>
        </section>
      </div>
      {confirmDialog}
    </dialog>
  );
}
