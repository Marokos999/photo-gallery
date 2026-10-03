import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { Navbar } from "./Navbar";

const navigation = vi.hoisted(() => ({ pathname: "/" }));
vi.mock("next/navigation", () => ({ usePathname: () => navigation.pathname }));

describe("Navbar", () => {
  it.each([
    ["/", "Albums"],
    ["/album", "Albums"],
    ["/search", "Search"],
    ["/upload", "Upload"],
  ])("marks the active link on %s", (pathname, label) => {
    navigation.pathname = pathname;
    render(<Navbar />);

    expect(screen.getByRole("link", { name: label })).toHaveAttribute("aria-current", "page");
    expect(screen.getAllByRole("link", { current: "page" })).toHaveLength(1);
  });

  it("hides owner navigation on public share pages", () => {
    navigation.pathname = "/shared";
    render(<Navbar />);

    expect(screen.queryByRole("link", { name: "Search" })).not.toBeInTheDocument();
    expect(screen.getByText("Photo Gallery")).toBeInTheDocument();
  });
});
