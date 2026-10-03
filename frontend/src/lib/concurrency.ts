/**
 * Runs `worker` over `items` with at most `limit` running at the same time.
 * Keeps large drops (e.g. 100 photos) from opening 100 parallel uploads.
 */
export async function forEachWithLimit<T>(
  items: readonly T[],
  limit: number,
  worker: (item: T) => Promise<void>,
): Promise<void> {
  const queue = [...items];
  const lanes = Array.from({ length: Math.min(limit, queue.length) }, async () => {
    for (let item = queue.shift(); item !== undefined; item = queue.shift()) await worker(item);
  });
  await Promise.all(lanes);
}
