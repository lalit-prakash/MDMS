"use client";

import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, Typography, Grid, Box, Stack } from "@mui/material";
import ElectricMeterOutlinedIcon from "@mui/icons-material/ElectricMeterOutlined";
import SupportAgentOutlinedIcon from "@mui/icons-material/SupportAgentOutlined";
import ShieldOutlinedIcon from "@mui/icons-material/ShieldOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import ErrorOutlineOutlinedIcon from "@mui/icons-material/ErrorOutlineOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import { apiClient } from "@/lib/apiClient";
import { Meter, LoadSurveyInterval, DailyLoadProfile, VeeExecutionRecord, MeasurementQuality } from "@/lib/types";
import { ReportResult } from "@/lib/reports/types";
import { Complaint } from "@/lib/complaints";
import { RevenueProtectionLead } from "@/lib/revenueProtection";
import { KpiCard } from "@/components/KpiCard";
import { DonutChart } from "@/components/DonutChart";
import { TrendLineChart } from "@/components/TrendLineChart";
import { useAuth } from "@/lib/session/AuthContext";
import { getGreeting } from "@/lib/greeting";
import { useThemeMode } from "@/lib/theme/ThemeModeContext";

interface NetworkEnergyReading {
  id: string;
  hierarchyNodeId: string;
  date: string;
  energyKwh: number;
}

// Mirrored by hand from the same badge tokens in globals.css — DonutChart/TrendLineChart render
// via ECharts, which can't reliably resolve CSS var() strings, so real per-mode hex is needed here.
const QUALITY_COLOR: Record<MeasurementQuality, { light: string; dark: string }> = {
  Valid: { light: "#16803C", dark: "#56D364" },
  Suspect: { light: "#9A6700", dark: "#E3B341" },
  OutOfRange: { light: "#CF222E", dark: "#FF7B72" },
  Missing: { light: "#0969DA", dark: "#79C0FF" },
  NegativeConsumption: { light: "#6F7D83", dark: "#7F8D91" },
};

function countBy<T extends string>(items: T[]): Partial<Record<T, number>> {
  const out: Partial<Record<T, number>> = {};
  for (const item of items) out[item] = (out[item] ?? 0) + 1;
  return out;
}

export default function HomePage() {
  const { user } = useAuth();
  const { mode } = useThemeMode();

  // Every number and chart below is computed from an endpoint the app already calls elsewhere
  // (meters, complaints, revenue-protection leads, meter-data quality holds, LS/DLP intervals, VEE
  // execution records, network energy readings) — queried again here at the dashboard level, never
  // fabricated. Anything the mockup showed with no real backing data (a previous-period comparison,
  // ₹ revenue-at-risk, per-item alert timestamps) was left out rather than invented.
  const metersQuery = useQuery({ queryKey: ["dashboard-meters"], queryFn: () => apiClient.get<Meter[]>("/api/v1/meters") });
  const complaintsQuery = useQuery({ queryKey: ["dashboard-complaints"], queryFn: () => apiClient.get<Complaint[]>("/api/v1/complaints") });
  const leadsQuery = useQuery({
    queryKey: ["dashboard-revenue-protection-leads"],
    queryFn: () => apiClient.get<RevenueProtectionLead[]>("/api/v1/revenue-protection/leads"),
  });
  const holdsQuery = useQuery({
    queryKey: ["dashboard-data-quality-holds"],
    queryFn: () => apiClient.get<unknown[]>("/api/v1/meter-data/billing-holds?activeOnly=true"),
  });
  // These endpoints paginate now (max 100 rows/page) — the dashboard's quality-mix donut reads
  // that one page as a real, if capped, sample rather than every LS/DLP row in the system.
  const lsQuery = useQuery({
    queryKey: ["dashboard-ls"],
    queryFn: () => apiClient.get<ReportResult<LoadSurveyInterval, undefined>>("/api/v1/meter-data/ls?pageSize=100"),
  });
  const dlpQuery = useQuery({
    queryKey: ["dashboard-dlp"],
    queryFn: () => apiClient.get<ReportResult<DailyLoadProfile, undefined>>("/api/v1/meter-data/dlp?pageSize=100"),
  });
  const veeQuery = useQuery({
    queryKey: ["dashboard-vee-executions"],
    queryFn: () => apiClient.get<VeeExecutionRecord[]>("/api/v1/vee/execution-records"),
  });
  const readingsQuery = useQuery({
    queryKey: ["dashboard-network-energy-readings"],
    queryFn: () => apiClient.get<NetworkEnergyReading[]>("/api/v1/energy-audit/network-energy-readings"),
  });

  const openComplaints = complaintsQuery.data?.filter((c) => c.status !== "Resolved" && c.status !== "Closed").length;
  const openLeads = leadsQuery.data?.filter((l) => l.status !== "Closed").length;

  const veePassed = veeQuery.data?.filter((v) => v.resultQuality === "Valid").length ?? 0;
  const veeTotal = veeQuery.data?.length ?? 0;
  const veePassRate = veeTotal > 0 ? Math.round((veePassed / veeTotal) * 1000) / 10 : null;

  const qualityCounts = countBy<MeasurementQuality>([
    ...(lsQuery.data?.data.map((r) => r.quality) ?? []),
    ...(dlpQuery.data?.data.map((r) => r.quality) ?? []),
  ]);
  const qualityTotal = Object.values(qualityCounts).reduce((a, b) => a + (b ?? 0), 0);
  const qualitySegments = (Object.keys(qualityCounts) as MeasurementQuality[])
    .filter((k) => (qualityCounts[k] ?? 0) > 0)
    .map((k) => ({ label: k, value: qualityCounts[k] ?? 0, color: QUALITY_COLOR[k][mode] }));

  const veeSegments = veeTotal > 0
    ? [
        { label: "Passed", value: veePassed, color: QUALITY_COLOR.Valid[mode] },
        { label: "Failed", value: veeTotal - veePassed, color: QUALITY_COLOR.OutOfRange[mode] },
      ]
    : [];

  const highRiskMeters = new Set(
    leadsQuery.data?.filter((l) => l.riskScore >= 70 && l.status !== "Closed").map((l) => l.meterId ?? l.customerId)
  ).size;
  const investigations = leadsQuery.data?.filter((l) => l.status === "FieldInvestigation").length ?? 0;
  const resolvedLeads = leadsQuery.data?.filter((l) => l.status === "Closed").length ?? 0;

  const readingsByDate = new Map<string, number>();
  for (const r of readingsQuery.data ?? []) {
    readingsByDate.set(r.date, (readingsByDate.get(r.date) ?? 0) + r.energyKwh);
  }
  const trendDates = [...readingsByDate.keys()].sort();
  const trendValues = trendDates.map((d) => readingsByDate.get(d) ?? 0);

  const urgentComplaints = [...(complaintsQuery.data ?? [])]
    .filter((c) => c.status !== "Resolved" && c.status !== "Closed")
    .sort((a, b) => new Date(a.slaDueUtc).getTime() - new Date(b.slaDueUtc).getTime())
    .slice(0, 3);
  const riskiestLeads = [...(leadsQuery.data ?? [])]
    .filter((l) => l.status !== "Closed")
    .sort((a, b) => b.riskScore - a.riskScore)
    .slice(0, 2);

  const today = new Date();

  return (
    <Box>
      <Stack direction={{ xs: "column", sm: "row" }} sx={{ mb: 4, justifyContent: "space-between", alignItems: { sm: "flex-end" }, gap: 1 }}>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 700 }}>
            {getGreeting()}, {user?.displayName?.split(/\s+/)[0] ?? ""} 👋
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Here&rsquo;s what&rsquo;s happening across MDMS today. No role-based landing pages yet — this hub shows every module.
          </Typography>
        </Box>
        <Typography variant="body2" color="text.secondary">
          {today.toLocaleDateString(undefined, { weekday: "long", day: "2-digit", month: "short", year: "numeric" })}
        </Typography>
      </Stack>

      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid size={{ xs: 6, md: 2.4 }}>
          <KpiCard
            label="Total Meters"
            value={metersQuery.data?.length.toLocaleString() ?? ""}
            loading={metersQuery.isPending}
            icon={<ElectricMeterOutlinedIcon fontSize="small" />}
            tone="brand"
          />
        </Grid>
        <Grid size={{ xs: 6, md: 2.4 }}>
          <KpiCard
            label="VEE Pass Rate"
            value={veePassRate !== null ? `${veePassRate}%` : "No data"}
            loading={veeQuery.isPending}
            icon={<CheckCircleOutlineOutlinedIcon fontSize="small" />}
            tone="success"
          />
        </Grid>
        <Grid size={{ xs: 6, md: 2.4 }}>
          <KpiCard
            label="Active Data-Quality Holds"
            value={holdsQuery.data?.length.toLocaleString() ?? ""}
            loading={holdsQuery.isPending}
            icon={<WarningAmberOutlinedIcon fontSize="small" />}
            tone="warning"
          />
        </Grid>
        <Grid size={{ xs: 6, md: 2.4 }}>
          <KpiCard
            label="Open Complaints"
            value={openComplaints?.toLocaleString() ?? ""}
            loading={complaintsQuery.isPending}
            icon={<SupportAgentOutlinedIcon fontSize="small" />}
            tone="info"
          />
        </Grid>
        <Grid size={{ xs: 6, md: 2.4 }}>
          <KpiCard
            label="Open Investigations"
            value={openLeads?.toLocaleString() ?? ""}
            loading={leadsQuery.isPending}
            icon={<ShieldOutlinedIcon fontSize="small" />}
            tone="error"
          />
        </Grid>
      </Grid>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, md: 8 }}>
          <Card variant="outlined" sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1 }}>
                Network Energy Readings
              </Typography>
              {trendDates.length > 0 ? (
                <TrendLineChart dates={trendDates} values={trendValues} seriesName="Energy (kWh)" />
              ) : (
                <Typography variant="body2" color="text.secondary" sx={{ py: 6, textAlign: "center" }}>
                  No network energy readings recorded yet.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card variant="outlined" sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
                Meter Data Quality
              </Typography>
              {qualityTotal > 0 ? (
                <DonutChart
                  segments={qualitySegments}
                  centerValue={`${Math.round(((qualityCounts.Valid ?? 0) / qualityTotal) * 100)}%`}
                  centerLabel="Valid"
                />
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No load survey / DLP records yet.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3} sx={{ mb: 5 }}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card variant="outlined" sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
                VEE Execution
              </Typography>
              {veeTotal > 0 ? (
                <DonutChart segments={veeSegments} centerValue={`${veePassRate}%`} centerLabel="Passed" />
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No VEE execution records yet.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card variant="outlined" sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
                Revenue Protection
              </Typography>
              <Stack direction="row" spacing={2}>
                <Box sx={{ flex: 1, textAlign: "center" }}>
                  <Typography sx={{ fontSize: 24, fontWeight: 700, color: "var(--badge-error-text)" }}>{highRiskMeters}</Typography>
                  <Typography variant="caption" color="text.secondary">High Risk</Typography>
                </Box>
                <Box sx={{ flex: 1, textAlign: "center" }}>
                  <Typography sx={{ fontSize: 24, fontWeight: 700, color: "var(--badge-warning-text)" }}>{investigations}</Typography>
                  <Typography variant="caption" color="text.secondary">Investigations</Typography>
                </Box>
                <Box sx={{ flex: 1, textAlign: "center" }}>
                  <Typography sx={{ fontSize: 24, fontWeight: 700, color: "var(--badge-success-text)" }}>{resolvedLeads}</Typography>
                  <Typography variant="caption" color="text.secondary">Resolved</Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Card variant="outlined" sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1.5 }}>
                Recent Alerts
              </Typography>
              <Stack spacing={1.25}>
                {urgentComplaints.map((c) => (
                  <Stack key={c.id} direction="row" spacing={1} sx={{ alignItems: "flex-start" }}>
                    <WarningAmberOutlinedIcon fontSize="small" sx={{ color: "var(--badge-warning-text)", mt: "2px" }} />
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {c.description.length > 48 ? `${c.description.slice(0, 48)}…` : c.description}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        SLA due {new Date(c.slaDueUtc).toLocaleDateString()}
                      </Typography>
                    </Box>
                  </Stack>
                ))}
                {riskiestLeads.map((l) => (
                  <Stack key={l.id} direction="row" spacing={1} sx={{ alignItems: "flex-start" }}>
                    <ErrorOutlineOutlinedIcon fontSize="small" sx={{ color: "var(--badge-error-text)", mt: "2px" }} />
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        High-risk lead — score {l.riskScore}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Status: {l.status}
                      </Typography>
                    </Box>
                  </Stack>
                ))}
                {urgentComplaints.length === 0 && riskiestLeads.length === 0 && (
                  <Typography variant="body2" color="text.secondary">
                    Nothing needs attention right now.
                  </Typography>
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
