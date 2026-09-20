"use client";

import { Box, Typography } from "@mui/material";
// MUI v9 icons-material only ships theme-suffixed variants (no bare "...Outline" export) — see
// the frontend README's MUI-v9-breaking-changes note for the other instance of this.
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import ErrorOutlineOutlinedIcon from "@mui/icons-material/ErrorOutlineOutlined";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import RadioButtonUncheckedOutlinedIcon from "@mui/icons-material/RadioButtonUncheckedOutlined";

export type SemanticCategory = "success" | "warning" | "error" | "info" | "neutral";

const ICONS: Record<SemanticCategory, typeof CheckCircleOutlineOutlinedIcon> = {
  success: CheckCircleOutlineOutlinedIcon,
  warning: WarningAmberOutlinedIcon,
  error: ErrorOutlineOutlinedIcon,
  info: InfoOutlinedIcon,
  neutral: RadioButtonUncheckedOutlinedIcon,
};

/**
 * Every domain status this app renders, mapped to one of the design spec's five badge
 * categories (§22-25). Color alone is never the indicator (§36/§7-1) — every badge also carries
 * an icon and the exact status text, e.g. "[!] VEE Failed", never a bare colored dot.
 */
const STATUS_CATEGORY: Record<string, SemanticCategory> = {
  // MeasurementQuality
  Valid: "success",
  NegativeConsumption: "error",
  OutOfRange: "warning",
  Missing: "neutral",
  Suspect: "warning",
  // MeasurementSource
  Received: "neutral",
  Estimated: "info",
  Edited: "info",
  Calculated: "neutral",
  // MeterStatus
  InStock: "neutral",
  Installed: "success",
  Removed: "warning",
  Retired: "neutral",
  // Data quality hold
  Active: "warning",
  Cleared: "success",
  // ComplaintStatus
  Open: "warning",
  Assigned: "info",
  InProgress: "info",
  Resolved: "success",
  Closed: "neutral",
  // RevenueProtectionLeadStatus
  Detected: "warning",
  Scored: "warning",
  Reviewed: "info",
  FieldInvestigation: "info",
  FindingRecorded: "info",
  ActionTaken: "warning",
  RecoveryRecorded: "success",
  // Prepaid connection
  Connected: "success",
  Disconnected: "error",
  // MeterEventSeverity
  Info: "info",
  Warning: "warning",
  Critical: "error",
  // MeterEvent acknowledgement status (Open already covered above via ComplaintStatus)
  Acknowledged: "success",
  // Download Request lifecycle
  Processing: "info",
  Completed: "success",
  Pending: "warning",
  Failed: "error",
  // LoadLimitState
  Normal: "success",
  Limited: "warning",
};

export function getSemanticCategory(value: string): SemanticCategory {
  return STATUS_CATEGORY[value] ?? "neutral";
}

/** Renders any domain status/quality/source value as a compact badge: color + icon + label. */
export function StatusBadge({ value, category }: { value: string; category?: SemanticCategory }) {
  const resolved = category ?? getSemanticCategory(value);
  const Icon = ICONS[resolved];

  return (
    <Box
      component="span"
      sx={{
        display: "inline-flex",
        alignItems: "center",
        gap: "var(--space-1)",
        px: "var(--space-2)",
        py: "2px",
        borderRadius: "var(--radius-pill)",
        bgcolor: `var(--badge-${resolved}-bg)`,
      }}
    >
      <Icon sx={{ fontSize: 14, color: `var(--badge-${resolved}-text)` }} />
      <Typography
        component="span"
        sx={{ fontSize: "11px", fontWeight: 500, color: `var(--badge-${resolved}-text)`, lineHeight: 1.4 }}
      >
        {value}
      </Typography>
    </Box>
  );
}
