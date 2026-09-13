"use client";

import ReactECharts from "echarts-for-react";
import { Box } from "@mui/material";
import { useThemeMode } from "@/lib/theme/ThemeModeContext";

// ECharts can't parse CSS var() strings reliably across renderers, so grid/axis colors are
// resolved here per theme mode instead — mirrored by hand from the same globals.css values.
const GRID_COLOR = { light: "#D9E0E3", dark: "rgba(155, 168, 171, 0.18)" };
const ACCENT = { light: "#0F9F91", dark: "#2DD4BF" };

export function TrendLineChart({
  dates,
  values,
  seriesName,
  height = 220,
}: {
  dates: string[];
  values: number[];
  seriesName: string;
  height?: number;
}) {
  const { mode } = useThemeMode();
  const gridColor = GRID_COLOR[mode];
  const color = ACCENT[mode];

  const option = {
    grid: { left: 48, right: 16, top: 24, bottom: 28 },
    tooltip: { trigger: "axis" },
    xAxis: { type: "category", data: dates, axisLine: { lineStyle: { color: gridColor } } },
    yAxis: { type: "value", splitLine: { lineStyle: { color: gridColor } } },
    series: [
      {
        name: seriesName,
        type: "line",
        data: values,
        smooth: true,
        showSymbol: false,
        lineStyle: { color, width: 2 },
        areaStyle: { color, opacity: 0.12 },
      },
    ],
  };

  return (
    <Box sx={{ width: "100%", height }}>
      <ReactECharts option={option} style={{ width: "100%", height }} opts={{ renderer: "svg" }} notMerge />
    </Box>
  );
}
