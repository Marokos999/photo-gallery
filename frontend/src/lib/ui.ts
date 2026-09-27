// Shared Tailwind class sets so buttons and inputs look the same everywhere.
export const button = {
  primary:
    "rounded-lg bg-neutral-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-neutral-700 disabled:opacity-50",
  secondary:
    "rounded-lg border border-neutral-300 bg-white px-4 py-2 text-sm font-medium transition hover:bg-neutral-100 disabled:opacity-50",
  danger:
    "rounded-lg border border-red-200 bg-white px-4 py-2 text-sm font-medium text-red-700 transition hover:bg-red-50 disabled:opacity-50",
} as const;

export const input =
  "w-full rounded-lg border border-neutral-300 bg-white px-3 py-2 text-sm focus:ring-2 focus:ring-neutral-400 focus:outline-none";

export function parseTags(text: string): string[] {
  return text
    .split(",")
    .map((tag) => tag.trim())
    .filter((tag) => tag.length > 0);
}

const MIN_TILE_RATIO = 3 / 4;
const MAX_TILE_RATIO = 2;

/**
 * Aspect ratio for grid tiles. Clamped so very wide or very tall images (e.g. terminal screenshots)
 * still get a usable tile; object-cover crops them in the grid, the lightbox shows them in full.
 */
export function tileAspectRatio(width: number | null, height: number | null): string {
  if (!width || !height) return "4 / 3";
  const ratio = Math.min(Math.max(width / height, MIN_TILE_RATIO), MAX_TILE_RATIO);
  return ratio.toFixed(3);
}
