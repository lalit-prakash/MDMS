"use client";

import Link from "next/link";
import { Box, Card, CardActionArea, CardContent, Grid, Typography } from "@mui/material";
import ElectricMeterOutlinedIcon from "@mui/icons-material/ElectricMeterOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import SupportAgentOutlinedIcon from "@mui/icons-material/SupportAgentOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import ShieldOutlinedIcon from "@mui/icons-material/ShieldOutlined";
import AssessmentOutlinedIcon from "@mui/icons-material/AssessmentOutlined";
import { PageHeader } from "@/components/PageHeader";

// Deliberately a flat hub, not scattered into the global sidebar — the reporting spec's own rule
// (§33/#19) is that the sidebar carries business modules, not a long list of individual reports.
// Only reports backed by data this system actually records are listed; the full 63-report
// catalogue from the spec needs data models (consumer master fields, device telemetry, a billing
// engine, ingestion-pipeline metadata) that don't exist yet.
const REPORTS = [
  { href: "/reports/meter-inventory", title: "Meter Inventory", description: "Current meter, consumer, and service-point mapping.", icon: ElectricMeterOutlinedIcon, id: "RPT-MTR-001" },
  { href: "/reports/vee-execution", title: "VEE Execution", description: "Every validation/estimation/editing run and its resulting quality.", icon: FactCheckOutlinedIcon, id: "RPT-VEE-001" },
  { href: "/reports/complaint-register", title: "Complaint Register", description: "All complaints with SLA due dates and resolution status.", icon: SupportAgentOutlinedIcon, id: "RPT-COMP-001" },
  { href: "/reports/prepaid-balance", title: "Prepaid Balance", description: "Current wallet balance and connection status per consumer.", icon: AccountBalanceWalletOutlinedIcon, id: "RPT-PREPAID-001" },
  { href: "/reports/revenue-risk", title: "Revenue Risk", description: "Scored revenue-protection leads and recovery status.", icon: ShieldOutlinedIcon, id: "RPT-RP-001" },
];

export default function ReportsHubPage() {
  return (
    <Box>
      <PageHeader icon={<AssessmentOutlinedIcon fontSize="small" />} title="Reports" />
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Server-side paginated, filterable, CSV-exportable reports. This is the first slice of the
        full reporting catalogue — only reports backed by data this system actually records.
      </Typography>
      <Grid container spacing={3}>
        {REPORTS.map((r) => {
          const Icon = r.icon;
          return (
            <Grid key={r.href} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card variant="outlined" sx={{ height: "100%" }}>
                <CardActionArea component={Link} href={r.href} sx={{ height: "100%", p: 1 }}>
                  <CardContent>
                    <Box
                      sx={{
                        display: "flex", alignItems: "center", justifyContent: "center",
                        width: 36, height: 36, borderRadius: "var(--radius-md)",
                        bgcolor: "var(--color-accent-soft)", color: "var(--color-accent)", mb: "var(--space-3)",
                      }}
                    >
                      <Icon fontSize="small" />
                    </Box>
                    <Typography variant="h6" gutterBottom>{r.title}</Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>{r.description}</Typography>
                    <Typography variant="caption" sx={{ color: "var(--card-muted)", fontFamily: "var(--font-mono)" }}>{r.id}</Typography>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          );
        })}
      </Grid>
    </Box>
  );
}
