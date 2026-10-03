"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useDropzone, type FileRejection } from "react-dropzone";
import { ApiError, api, uploadFile } from "@/lib/api";
import { forEachWithLimit } from "@/lib/concurrency";

const MAX_FILE_BYTES = 25 * 1024 * 1024;
const ACCEPTED_TYPES = { "image/jpeg": [], "image/png": [], "image/webp": [] };
const MAX_PARALLEL_UPLOADS = 3;

type UploadStatus = "queued" | "uploading" | "done" | "error";

interface UploadItem {
  id: string;
  name: string;
  progress: number;
  status: UploadStatus;
  error?: string;
}

interface UploadDropzoneProps {
  albumId: string;
  caption?: string;
  tags?: string[];
}

export function UploadDropzone({ albumId, caption, tags }: UploadDropzoneProps) {
  const [uploads, setUploads] = useState<UploadItem[]>([]);
  const queryClient = useQueryClient();

  function update(id: string, changes: Partial<UploadItem>) {
    setUploads((current) => current.map((item) => (item.id === id ? { ...item, ...changes } : item)));
  }

  async function uploadOne({ id, file }: { id: string; file: File }) {
    update(id, { status: "uploading" });

    try {
      const ticket = await api.requestUpload({ fileName: file.name, contentType: file.type, albumId, caption, tags });
      await uploadFile(ticket, file, (progress) => update(id, { progress }));
      update(id, { status: "done", progress: 1 });
      // The new photo shows up as "Processing…"; the album page polls until it is ready.
      await queryClient.invalidateQueries({ queryKey: ["photos", albumId] });
    } catch (error) {
      const message =
        error instanceof ApiError && error.status === 400
          ? "Rejected by the server (check caption and tags)."
          : "Upload failed.";
      update(id, { status: "error", error: message });
    }
  }

  function onDrop(accepted: File[], rejected: FileRejection[]) {
    for (const rejection of rejected) {
      setUploads((current) => [
        {
          id: crypto.randomUUID(),
          name: rejection.file.name,
          progress: 0,
          status: "error",
          error: "Only JPEG, PNG or WebP up to 25 MB.",
        },
        ...current,
      ]);
    }
    const queued = accepted.map((file) => ({ id: crypto.randomUUID(), file }));
    setUploads((current) => [
      ...queued.map(({ id, file }): UploadItem => ({ id, name: file.name, progress: 0, status: "queued" })),
      ...current,
    ]);
    void forEachWithLimit(queued, MAX_PARALLEL_UPLOADS, uploadOne);
  }

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: ACCEPTED_TYPES,
    maxSize: MAX_FILE_BYTES,
    disabled: albumId === "",
  });

  return (
    <div className="space-y-3">
      <div
        {...getRootProps()}
        className={`cursor-pointer rounded-xl border-2 border-dashed p-8 text-center transition ${
          isDragActive ? "border-neutral-900 bg-neutral-100" : "border-neutral-300 bg-white hover:border-neutral-400"
        } ${albumId === "" ? "cursor-not-allowed opacity-50" : ""}`}
      >
        <input {...getInputProps()} />
        <p className="font-medium">{isDragActive ? "Drop the photos here" : "Drop photos here or click to choose"}</p>
        <p className="text-sm text-neutral-500">JPEG, PNG or WebP · up to 25 MB each</p>
      </div>

      {uploads.length > 0 && (
        <ul className="space-y-2">
          {uploads.map((item) => (
            <li key={item.id} className="rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm">
              <div className="flex items-center justify-between gap-3">
                <span className="truncate">{item.name}</span>
                <span
                  className={
                    item.status === "error"
                      ? "text-red-600"
                      : item.status === "done"
                        ? "text-green-700"
                        : "text-neutral-500"
                  }
                >
                  {item.status === "queued" && "Queued"}
                  {item.status === "uploading" && `${Math.round(item.progress * 100)}%`}
                  {item.status === "done" && "Uploaded"}
                  {item.status === "error" && item.error}
                </span>
              </div>
              {item.status === "uploading" && (
                <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-neutral-200">
                  <div className="h-full bg-neutral-900 transition-all" style={{ width: `${item.progress * 100}%` }} />
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
