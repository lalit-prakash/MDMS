"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { Card, CardActionArea, CardContent, Typography, Grid, Box } from "@mui/material";
import ElectricMeterOutlinedIcon from "@mui/icons-material/ElectricMeterOutlined";
import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import InsightsOutlinedIcon from "@mui/icons-material/InsightsOutlined";
import SupportAgentOutlinedIcon from "@mui/icons-material/SupportAgentOutlined";
import ShieldOutlinedIcon from "@mui/icons-material/ShieldOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import { apiClient } from "@/lib/apiClient";
import { Meter } from "@/lib/types";
import { Complaint } from "@/lib/complaints";
import { RevenueProtectionLead } from "@/lib/revenueProtection";
import { KpiCard } from "@/components/KpiCard";

const TILES = [
  {
    href: "/meters",
    title: "Meters",
    description: "Meter master data, installation and replacement history.",
    icon: ElectricMeterOutlinedIcon,
  },
  {
    href: "/meter-data",
    title: "Meter Data",
    description: "Load Survey and Daily Load Profile ingestion, data-quality holds.",
    icon: StorageOutlinedIcon,
  },
  {
    href: "/vee",
    title: "VEE",
    description: "Out-of-range thresholds, missing-interval estimation, execution audit trail.",
    icon: FactCheckOutlinedIcon,
  },
  {
    href: "/config",
    title: "Config",
    description: "Tariff categories, electrical hierarchy, organizational hierarchy.",
    icon: TuneOutlinedIcon,
  },
  {
    href: "/users",
    title: "Users",
    description: "User records, roles, and org-unit scoping (RBAC data — not yet enforced).",
    icon: GroupOutlinedIcon,
  },
  {
    href: "/energy-audit",
    title: "Energy Audit",
    description: "Network energy readings and transparent supply-vs-consumption balance.",
    icon: InsightsOutlinedIcon,
  },
  {
    href: "/complaints",
    title: "Complaints",
    description: "Consumer complaint tickets with SLA tracking through resolution and closure.",
    icon: SupportAgentOutlinedIcon,
  },
  {
    href: "/revenue-protection",
    title: "Revenue Protection",
    description: "Risk signals, scored leads, and an investigation workflow through closure.",
    icon: ShieldOutlinedIcon,
  },
  {
    href: "/prepaid",
    title: "Prepaid",
    description: "Wallet balance, idempotent recharge, daily billing, and connect/disconnect.",
    icon: AccountBalanceWalletOutlinedIcon,
  },
];

export default function HomePage() {
  // Every number below is a real count from an endpoint the app already calls elsewhere — none of
  // it is sample/placeholder data. If a call fails (e.g. backend not running), the card shows "—"
  // via KpiCard's `loading` state rather than a fabricated number.
  const metersQuery = useQuery({
    queryKey: ["dashboard-meters"],
    queryFn: () => apiClient.get<Meter[]>("/api/v1/meters"),
  });
  const complaintsQuery = useQuery({
    queryKey: ["dashboard-complaints"],
    queryFn: () => apiClient.get<Complaint[]>("/api/v1/complaints"),
  });
  const leadsQuery = useQuery({
    queryKey: ["dashboard-revenue-protection-leads"],
    queryFn: () => apiClient.get<RevenueProtectionLead[]>("/api/v1/revenue-protection/leads"),
  });
  const holdsQuery = useQuery({
    queryKey: ["dashboard-data-quality-holds"],
    queryFn: () => apiClient.get<unknown[]>("/api/v1/meter-data/billing-holds?activeOnly=true"),
  });

  const openComplaints = complaintsQuery.data?.filter((c) => c.status !== "Resolved" && c.status !== "Closed").length;
  const openLeads = leadsQuery.data?.filter((l) => l.status !== "Closed").length;

  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ fontWeight: 600 }}>
        Dashboard
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        No role-based landing pages yet — this hub shows every module until authentication exists.
      </Typography>

      <Grid container spacing={3} sx={{ mb: 5 }}>
        <Grid size={{ xs: 6, md: 3 }}>
          <KpiCard
            label="Total Meters"
            value={metersQuery.data?.length.toLocaleString() ?? ""}
            loading={metersQuery.isPending}
            icon={<ElectricMeterOutlinedIcon fontSize="small" />}
            tone="brand"
          />
        </Grid>
        <Grid size={{ xs: 6, md: 3 }}>
          <KpiCard
            label="Open Complaints"
            value={openComplaints?.toLocaleString() ?? ""}
            loading={complaintsQuery.isPending}
            icon={<SupportAgentOutlinedIcon fontSize="small" />}
            tone="warning"
          />
        </Grid>
        <Grid size={{ xs: 6, md: 3 }}>
          <KpiCard
            label="Open Investigations"
            value={openLeads?.toLocaleString() ?? ""}
            loading={leadsQuery.isPending}
            icon={<ShieldOutlinedIcon fontSize="small" />}
            tone="error"
          />
        </Grid>
        <Grid size={{ xs: 6, md: 3 }}>
          <KpiCard
            label="Active Data-Quality Holds"
            value={holdsQuery.data?.length.toLocaleString() ?? ""}
            loading={holdsQuery.isPending}
            icon={<FactCheckOutlinedIcon fontSize="small" />}
            tone="info"
          />
        </Grid>
      </Grid>

      <Typography variant="h6" sx={{ mb: 2 }}>
        Modules
      </Typography>
      <Grid container spacing={3}>
        {TILES.map((tile) => {
          const Icon = tile.icon;
          return (
            <Grid key={tile.href} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card variant="outlined" sx={{ height: "100%" }}>
                <CardActionArea component={Link} href={tile.href} sx={{ height: "100%", p: 1 }}>
                  <CardContent>
                    <Box
                      sx={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        width: 36,
                        height: 36,
                        borderRadius: "var(--radius-md)",
                        bgcolor: "var(--color-accent-soft)",
                        color: "var(--color-accent)",
                        mb: "var(--space-3)",
                      }}
                    >
                      <Icon fontSize="small" />
                    </Box>
                    <Typography variant="h6" gutterBottom>
                      {tile.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {tile.description}
                    </Typography>
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
