import { describe, expect, it } from "vitest";
import { forEachWithLimit } from "./concurrency";

const tick = () => new Promise((resolve) => setTimeout(resolve, 1));

describe("forEachWithLimit", () => {
  it("processes every item and never exceeds the limit", async () => {
    let running = 0;
    let peak = 0;
    const done: number[] = [];

    await forEachWithLimit([1, 2, 3, 4, 5, 6, 7], 3, async (item) => {
      running++;
      peak = Math.max(peak, running);
      await tick();
      done.push(item);
      running--;
    });

    expect(done.sort()).toEqual([1, 2, 3, 4, 5, 6, 7]);
    expect(peak).toBe(3);
  });

  it("handles an empty list", async () => {
    await expect(forEachWithLimit([], 3, async () => {})).resolves.toBeUndefined();
  });
});
