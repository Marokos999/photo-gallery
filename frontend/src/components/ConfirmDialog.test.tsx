import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { useConfirm } from "./ConfirmDialog";

function Harness() {
  const [confirm, confirmDialog] = useConfirm();
  const [result, setResult] = useState("none");

  async function ask() {
    const confirmed = await confirm({ title: "Delete photo?", message: "This cannot be undone." });
    setResult(String(confirmed));
  }

  return (
    <>
      <button onClick={() => void ask()}>Ask</button>
      <output>{result}</output>
      {confirmDialog}
    </>
  );
}

describe("useConfirm", () => {
  it("resolves true when the action is confirmed", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(screen.getByRole("button", { name: "Ask" }));
    expect(screen.getByRole("dialog", { name: "Delete photo?" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Delete" }));

    expect(screen.getByRole("status")).toHaveTextContent("true");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("resolves false on cancel and focuses Cancel first", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(screen.getByRole("button", { name: "Ask" }));
    expect(screen.getByRole("button", { name: "Cancel" })).toHaveFocus();

    await user.click(screen.getByRole("button", { name: "Cancel" }));

    expect(screen.getByRole("status")).toHaveTextContent("false");
  });
});
