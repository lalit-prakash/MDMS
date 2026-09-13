"use client";

import { useEffect, useState } from "react";
import { flushSync } from "react-dom";
import { Card, CardContent, Typography } from "@mui/material";
import { withViewTransition } from "@/lib/viewTransition";

/**
 * A KPI/summary stat card whose value cross-fades to a new number instead of snapping, when a
 * TanStack Query refetch lands a changed value — not on first mount. `transitionName` must be
 * unique per card instance on the page (the View Transitions spec fails the whole transition if
 * two elements share a `view-transition-name` in the same snapshot).
 */
export function MorphingStat({
  label,
  value,
  transitionName,
}: {
  label: string;
  value: string;
  transitionName: string;
}) {
  const [display, setDisplay] = useState(value);

  useEffect(() => {
    if (display === value) return;
    const commit = () => setDisplay(value);
    withViewTransition(() => flushSync(commit));
    // `display` deliberately excluded — this effect only reacts to incoming `value` changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value]);

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="caption" sx={{ color: "var(--card-muted)" }}>
          {label}
        </Typography>
        <Typography
          variant="h2"
          sx={{ color: "var(--card-metric)", fontSize: { xs: "28px", md: "32px" } }}
          style={{ viewTransitionName: transitionName } as React.CSSProperties}
        >
          {display}
        </Typography>
      </CardContent>
    </Card>
  );
}
