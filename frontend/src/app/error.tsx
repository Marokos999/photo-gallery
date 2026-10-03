"use client";

import { useEffect } from "react";
import { button } from "@/lib/ui";

export default function ErrorPage({ error, retry }: { error: Error & { digest?: string }; retry: () => void }) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <section className="mx-auto max-w-md space-y-4 py-16 text-center">
      <h1 className="text-2xl font-semibold">Something went wrong</h1>
      <p className="text-neutral-500">An unexpected error occurred while rendering this page.</p>
      <button type="button" onClick={retry} className={button.primary}>
        Try again
      </button>
    </section>
  );
}
