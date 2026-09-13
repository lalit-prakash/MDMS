"use client";

import ReactECharts from "echarts-for-react";
import { Box, Stack, Typography } from "@mui/material";

export interface DonutSegment {
  label: string;
  value: number;
  color: string;
}

/**
 * A donut chart with a big centered percentage and a legend, matching the design mockup's
 * "VEE Execution" / "Meter Data Quality" cards. Always built from real query results passed in by
 * the caller — this component has no data of its own.
 */
export function DonutChart({
  segments,
  centerValue,
  centerLabel,
  height = 160,
}: {
  segments: DonutSegment[];
  centerValue: string;
  centerLabel?: string;
  height?: number;
}) {
  const total = segments.reduce((sum, s) => sum + s.value, 0);

  const option = {
    tooltip: { trigger: "item", formatter: "{b}: {c} ({d}%)" },
    series: [
      {
        type: "pie",
        radius: ["70%", "92%"],
        avoidLabelOverlap: false,
        label: { show: false },
        labelLine: { show: false },
        data: segments.map((s) => ({ name: s.label, value: s.value, itemStyle: { color: s.color } })),
      },
    ],
  };

  return (
    <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
      <Box sx={{ position: "relative", width: height, height, flexShrink: 0 }}>
        {total > 0 ? (
          <ReactECharts option={option} style={{ width: height, height }} opts={{ renderer: "svg" }} />
        ) : (
          <Box
            sx={{
              width: height,
              height,
              borderRadius: "50%",
              border: "10px solid var(--color-border-default)",
            }}
          />
        )}
        <Box
          sx={{
            position: "absolute",
            inset: 0,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexDirection: "column",
          }}
        >
          <Typography sx={{ fontSize: 22, fontWeight: 700, color: "var(--card-metric)" }}>{centerValue}</Typography>
          {centerLabel && (
            <Typography sx={{ fontSize: 11, color: "var(--card-muted)" }}>{centerLabel}</Typography>
          )}
        </Box>
      </Box>
      <Stack spacing={0.75}>
        {segments.map((s) => (
          <Stack key={s.label} direction="row" spacing={1} sx={{ alignItems: "center" }}>
            <Box sx={{ width: 8, height: 8, borderRadius: "50%", bgcolor: s.color, flexShrink: 0 }} />
            <Typography variant="caption" sx={{ color: "var(--card-muted)" }}>
              {s.label}
            </Typography>
            <Typography variant="caption" sx={{ fontWeight: 600, color: "var(--card-metric)" }}>
              {s.value}
            </Typography>
          </Stack>
        ))}
      </Stack>
    </Box>
  );
}
