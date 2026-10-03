import Link from "next/link";
import { button } from "@/lib/ui";

export default function NotFound() {
  return (
    <section className="mx-auto max-w-md space-y-4 py-16 text-center">
      <h1 className="text-2xl font-semibold">Page not found</h1>
      <p className="text-neutral-500">The page you are looking for does not exist.</p>
      <Link href="/" className={`inline-block ${button.primary}`}>
        Back to albums
      </Link>
    </section>
  );
}
