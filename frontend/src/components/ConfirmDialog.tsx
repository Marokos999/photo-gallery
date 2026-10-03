"use client";

import { useEffect, useRef, useState } from "react";
import { button } from "@/lib/ui";

interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel?: string;
}

interface PendingConfirm extends ConfirmOptions {
  resolve: (confirmed: boolean) => void;
}

/**
 * Promise-based replacement for window.confirm with an accessible, styled <dialog>.
 *
 *   const [confirm, confirmDialog] = useConfirm();
 *   if (await confirm({ title: "Delete photo?", message: "…" })) { … }
 *   return <>{…}{confirmDialog}</>;
 */
export function useConfirm() {
  const [pending, setPending] = useState<PendingConfirm | null>(null);

  function confirm(options: ConfirmOptions) {
    return new Promise<boolean>((resolve) => setPending({ ...options, resolve }));
  }

  function settle(confirmed: boolean) {
    pending?.resolve(confirmed);
    setPending(null);
  }

  const dialog = pending ? <ConfirmDialog {...pending} onSettle={settle} /> : null;
  return [confirm, dialog] as const;
}

function ConfirmDialog({
  title,
  message,
  confirmLabel = "Delete",
  onSettle,
}: ConfirmOptions & { onSettle: (confirmed: boolean) => void }) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    dialogRef.current?.showModal();
  }, []);

  return (
    <dialog
      ref={dialogRef}
      onClose={() => onSettle(false)}
      aria-labelledby="confirm-title"
      className="m-auto w-full max-w-sm rounded-xl bg-white p-0 shadow-xl backdrop:bg-black/50"
    >
      <div className="space-y-4 p-6">
        <h2 id="confirm-title" className="text-lg font-semibold">
          {title}
        </h2>
        <p className="text-sm text-neutral-600">{message}</p>
        <div className="flex justify-end gap-2">
          {/* Cancel gets focus first, so Enter never deletes by accident. */}
          <button type="button" autoFocus onClick={() => onSettle(false)} className={button.secondary}>
            Cancel
          </button>
          <button type="button" onClick={() => onSettle(true)} className={button.dangerSolid}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </dialog>
  );
}
