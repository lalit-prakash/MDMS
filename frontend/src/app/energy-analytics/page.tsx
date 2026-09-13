"use client";

import { useQuery } from "@tanstack/react-query";
import { Alert, Box, Card, CardContent, Grid, Typography } from "@mui/material";
import InsightsOutlinedIcon from "@mui/icons-material/InsightsOutlined";
import { apiClient } from "@/lib/apiClient";
import { DailyLoadProfile } from "@/lib/types";
import { PageHeader } from "@/components/PageHeader";
import { TrendLineChart } from "@/components/TrendLineChart";

/**
 * Consumption analytics from Daily Load Profile data — the only interval-level consumption
 * numbers this system actually records. The reporting spec's Demand/Peak Demand/Load Factor
 * metrics need a kW demand field no entity here has (DailyLoadProfile only has consumptionKwh),
 * so those are left out rather than approximated from a field that doesn't exist.
 */
export default function EnergyAnalyticsPage() {
  const dlpQuery = useQuery({
    queryKey: ["energy-analytics-dlp"],
    queryFn: () => apiClient.get<DailyLoadProfile[]>("/api/v1/meter-data/dlp"),
  });

  const validRows = (dlpQuery.data ?? []).filter((r) => r.quality === "Valid");
  const byDate = new Map<string, number>();
  for (const r of validRows) {
    byDate.set(r.profileDate, (byDate.get(r.profileDate) ?? 0) + r.consumptionKwh);
  }
  const dates = [...byDate.keys()].sort();
  const values = dates.map((d) => byDate.get(d) ?? 0);

  const totalConsumption = values.reduce((a, b) => a + b, 0);
  const avgDaily = dates.length > 0 ? totalConsumption / dates.length : 0;
  const highestIdx = values.length > 0 ? values.indexOf(Math.max(...values)) : -1;
  const highestDay = highestIdx >= 0 ? dates[highestIdx] : null;
  const highestValue = highestIdx >= 0 ? values[highestIdx] : null;

  return (
    <Box>
      <PageHeader icon={<InsightsOutlinedIcon fontSize="small" />} title="Energy Analytics" />
      <Alert severity="info" sx={{ mb: 3 }}>
        Consumption metrics only, from valid Daily Load Profile records. Demand/Peak Demand/Load Factor aren&rsquo;t shown — no kW demand field exists in this system&rsquo;s data model yet.
      </Alert>

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 6, md: 4 }}>
          <Card variant="outlined">
            <CardContent>
              <Typography variant="caption" sx={{ color: "var(--card-muted)" }}>Total Consumption (Valid)</Typography>
              <Typography sx={{ fontSize: 24, fontWeight: 700, color: "var(--card-metric)" }}>{totalConsumption.toLocaleString()} kWh</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 6, md: 4 }}>
          <Card variant="outlined">
            <CardContent>
              <Typography variant="caption" sx={{ color: "var(--card-muted)" }}>Average Daily Consumption</Typography>
              <Typography sx={{ fontSize: 24, fontWeight: 700, color: "var(--card-metric)" }}>{avgDaily.toFixed(1)} kWh</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 6, md: 4 }}>
          <Card variant="outlined">
            <CardContent>
              <Typography variant="caption" sx={{ color: "var(--card-muted)" }}>Highest Consumption Day</Typography>
              <Typography sx={{ fontSize: 24, fontWeight: 700, color: "var(--card-metric)" }}>
                {highestDay ? `${highestValue?.toLocaleString()} kWh` : "—"}
              </Typography>
              {highestDay && (
                <Typography variant="caption" color="text.secondary">{highestDay}</Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1 }}>
            Daily Consumption Trend
          </Typography>
          {dates.length > 0 ? (
            <TrendLineChart dates={dates} values={values} seriesName="Consumption (kWh)" />
          ) : (
            <Typography variant="body2" color="text.secondary" sx={{ py: 6, textAlign: "center" }}>
              No valid Daily Load Profile records yet.
            </Typography>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
