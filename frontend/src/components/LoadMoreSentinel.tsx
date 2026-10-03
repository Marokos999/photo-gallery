"use client";

import { useEffect, useRef } from "react";

interface LoadMoreSentinelProps {
  onVisible: () => void;
  disabled: boolean;
}

/** Calls `onVisible` when the user scrolls near the end of the list (infinite scroll). */
export function LoadMoreSentinel({ onVisible, disabled }: LoadMoreSentinelProps) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element || disabled) return;

    const observer = new IntersectionObserver(([entry]) => entry?.isIntersecting && onVisible(), {
      rootMargin: "600px", // start loading before the user reaches the bottom
    });
    observer.observe(element);
    return () => observer.disconnect();
  }, [onVisible, disabled]);

  return <div ref={ref} aria-hidden className="h-px" />;
}
