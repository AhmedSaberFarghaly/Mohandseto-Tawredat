"use client";

import { useEffect, useState } from "react";

/**
 * Global top progress bar: lights up while any add/edit/delete
 * (non-GET fetch) request is in flight.
 */
export default function NetworkProgress() {
  const [active, setActive] = useState(false);

  useEffect(() => {
    const original = window.fetch;
    let inFlight = 0;
    window.fetch = async (...args: Parameters<typeof fetch>) => {
      const method = (
        args[1]?.method ??
        (args[0] instanceof Request ? args[0].method : "GET")
      ).toUpperCase();
      const mutating = method !== "GET" && method !== "HEAD";
      if (mutating) {
        inFlight++;
        setActive(true);
      }
      try {
        return await original(...args);
      } finally {
        if (mutating) {
          inFlight--;
          if (inFlight <= 0) setActive(false);
        }
      }
    };
    return () => {
      window.fetch = original;
    };
  }, []);

  return <div className={`net-progress${active ? " on" : ""}`} aria-hidden />;
}
