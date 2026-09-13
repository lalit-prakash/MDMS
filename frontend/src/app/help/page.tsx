"use client";

import { Box, Card, CardContent, Grid, Link as MuiLink, Typography } from "@mui/material";
import HelpOutlineOutlinedIcon from "@mui/icons-material/HelpOutlineOutlined";
import { PageHeader } from "@/components/PageHeader";

const MODULES = [
  { title: "Meters", description: "Meter master data, installation and replacement history." },
  { title: "Meter Data", description: "Load Survey and Daily Load Profile ingestion, data-quality holds." },
  { title: "VEE & Data Quality", description: "Out-of-range thresholds, missing-interval estimation, execution audit trail." },
  { title: "Energy Analytics", description: "Consumption trends from valid Daily Load Profile records." },
  { title: "Energy Audit", description: "Network energy readings and transparent supply-vs-consumption balance." },
  { title: "Revenue Protection", description: "Risk signals, scored leads, and an investigation workflow through closure." },
  { title: "Consumers", description: "Consumer and service-point master data." },
  { title: "Complaints", description: "Consumer complaint tickets with SLA tracking through resolution and closure." },
  { title: "Prepaid", description: "Wallet balance, idempotent recharge, daily billing, and connect/disconnect." },
  { title: "Configuration", description: "Tariff categories, electrical hierarchy, organizational hierarchy." },
  { title: "Users & Access", description: "User records, roles, and org-unit scoping." },
  { title: "Reports", description: "Server-side paginated, filterable, CSV-exportable reports." },
];

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5004";

export default function HelpPage() {
  return (
    <Box>
      <PageHeader icon={<HelpOutlineOutlinedIcon fontSize="small" />} title="Help & Support" />
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Need help? Contact your system administrator — there is no in-app support desk yet.
      </Typography>

      <Card variant="outlined" sx={{ mb: 4, maxWidth: 480 }}>
        <CardContent>
          <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1 }}>
            API Reference
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            The backend&rsquo;s interactive API docs (development only):
          </Typography>
          <MuiLink href={`${API_BASE_URL}/swagger`} target="_blank" rel="noreferrer">
            {API_BASE_URL}/swagger
          </MuiLink>
        </CardContent>
      </Card>

      <Typography variant="h6" sx={{ mb: 2 }}>
        Modules
      </Typography>
      <Grid container spacing={2}>
        {MODULES.map((m) => (
          <Grid key={m.title} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card variant="outlined" sx={{ height: "100%" }}>
              <CardContent>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>{m.title}</Typography>
                <Typography variant="body2" color="text.secondary">{m.description}</Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
