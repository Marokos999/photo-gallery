import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { Providers } from "./providers";
import Link from "next/link";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Photo Gallery",
  description: "Upload, organize and share photos in albums.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}>
      <body className="min-h-full bg-neutral-50 font-sans text-neutral-900">
        <Providers>
          <header className="border-b border-neutral-200 bg-white">
            <nav className="mx-auto flex max-w-6xl items-center px-4 py-3">
              <Link href="/" className="text-lg font-semibold tracking-tight">
              Photo Gallery
              </Link>
            </nav>
          </header>
          <main className="mx-auto max-w-6xl px-4 -y-8">{children}</main>
        </Providers>
      </body>
    </html>
  );
}
