"use client";

import { Chip } from "@mui/material";
import { qualityColor } from "@/app/providers";

/** Renders any data-quality/source/status value with the app's one shared color language. */
export function QualityChip({ value }: { value: string }) {
  const color = (qualityColor as Record<string, string>)[value] ?? "#616161";
  return (
    <Chip
      label={value}
      size="small"
      sx={{
        bgcolor: `${color}1a`,
        color,
        fontWeight: 600,
        border: `1px solid ${color}55`,
      }}
    />
  );
}
