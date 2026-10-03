import { describe, expect, it } from "vitest";
import { parseTags, tileAspectRatio } from "./ui";

describe("parseTags", () => {
  it("splits on commas, trims and drops empty entries", () => {
    expect(parseTags(" beach, sunset ,, , family ")).toEqual(["beach", "sunset", "family"]);
  });

  it("returns an empty list for blank input", () => {
    expect(parseTags("   ")).toEqual([]);
  });
});

describe("tileAspectRatio", () => {
  it("falls back to 4:3 when dimensions are unknown", () => {
    expect(tileAspectRatio(null, null)).toBe("4 / 3");
    expect(tileAspectRatio(1200, 0)).toBe("4 / 3");
  });

  it("keeps ordinary ratios", () => {
    expect(tileAspectRatio(1600, 1200)).toBe("1.333");
  });

  it("clamps very wide and very tall images", () => {
    expect(tileAspectRatio(1093, 133)).toBe("2.000");
    expect(tileAspectRatio(100, 1000)).toBe("0.750");
  });
});
