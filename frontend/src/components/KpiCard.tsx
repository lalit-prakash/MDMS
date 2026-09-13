import { Box, Card, CardContent, Typography } from "@mui/material";

export type KpiTone = "brand" | "success" | "warning" | "error" | "info";

const TONE_BG: Record<KpiTone, string> = {
  brand: "var(--badge-info-bg)",
  success: "var(--badge-success-bg)",
  warning: "var(--badge-warning-bg)",
  error: "var(--badge-error-bg)",
  info: "var(--badge-info-bg)",
};

const TONE_TEXT: Record<KpiTone, string> = {
  brand: "var(--color-accent)",
  success: "var(--badge-success-text)",
  warning: "var(--badge-warning-text)",
  error: "var(--badge-error-text)",
  info: "var(--badge-info-text)",
};

/** A single KPI stat card for the dashboard: a value backed by a real query, never a fabricated number. */
export function KpiCard({
  label,
  value,
  icon,
  tone = "brand",
  loading,
}: {
  label: string;
  value: string;
  icon: React.ReactNode;
  tone?: KpiTone;
  loading?: boolean;
}) {
  return (
    <Card variant="outlined" sx={{ height: "100%" }}>
      <CardContent sx={{ display: "flex", flexDirection: "column", gap: "var(--space-3)" }}>
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <Typography variant="caption" sx={{ color: "var(--card-muted)", fontWeight: 600 }}>
            {label}
          </Typography>
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              width: 32,
              height: 32,
              borderRadius: "var(--radius-md)",
              bgcolor: TONE_BG[tone],
              color: TONE_TEXT[tone],
            }}
          >
            {icon}
          </Box>
        </Box>
        <Typography variant="h2" sx={{ color: "var(--card-metric)", fontSize: { xs: "26px", md: "30px" } }}>
          {loading ? "—" : value}
        </Typography>
      </CardContent>
    </Card>
  );
}
