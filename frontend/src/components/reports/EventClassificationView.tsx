"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Box, Button, Card, CardContent, Grid, IconButton, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from "@mui/material";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import BoltOutlinedIcon from "@mui/icons-material/BoltOutlined";
import PowerOutlinedIcon from "@mui/icons-material/PowerOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import WifiOutlinedIcon from "@mui/icons-material/WifiOutlined";
import CategoryOutlinedIcon from "@mui/icons-material/CategoryOutlined";
import { apiClient } from "@/lib/apiClient";
import { ReportColumn } from "./ReportTable";
import { MeterDataListTab } from "./MeterDataListTab";
import { StatusBadge } from "@/components/StatusBadge";

interface ClassificationSummaryRow {
  classification: string;
  eventType: string;
  count: number;
  lastOccurrenceUtc: string;
}

interface ClassificationSummaryResponse {
  totalsByClassification: Record<string, number>;
  summary: ClassificationSummaryRow[];
  generatedAtUtc: string;
}

interface MeterEventRow {
  id: string;
  meterId: string;
  meterNumber: string;
  occurredAtUtc: string;
  eventType: string;
  classification: string;
  severity: string;
  description: string | null;
  occCurrent: number | null;
  occVoltage: number | null;
  occKwh: number | null;
  occTemperature: number | null;
  resolvedAtUtc: string | null;
  durationMinutes: number | null;
  isAcknowledged: boolean;
  acknowledgedAtUtc: string | null;
}

const CLASSIFICATIONS = [
  { key: "Voltage", label: "Voltage Events", icon: BoltOutlinedIcon, color: "var(--badge-warning-text)", bg: "var(--badge-warning-bg)" },
  { key: "Power", label: "Power Events", icon: PowerOutlinedIcon, color: "var(--badge-success-text)", bg: "var(--badge-success-bg)" },
  { key: "Security", label: "Security Events", icon: SecurityOutlinedIcon, color: "var(--badge-error-text)", bg: "var(--badge-error-bg)" },
  { key: "Control", label: "Control Events", icon: TuneOutlinedIcon, color: "var(--color-accent)", bg: "var(--color-accent-soft)" },
  { key: "Communication", label: "Communication Events", icon: WifiOutlinedIcon, color: "var(--badge-info-text)", bg: "var(--badge-info-bg)" },
  { key: "Other", label: "Other Events", icon: CategoryOutlinedIcon, color: "var(--card-muted)", bg: "var(--color-bg-surface-hover)" },
] as const;

/**
 * Two-level Events/Alarms view: a classification summary (KPI cards + one row per
 * classification/event-type with an aggregate count and last occurrence) that drills into the
 * individual occurrence rows for whichever (classification, event type) the user clicks —
 * matching the reference UI's Event Count by Classification / Event Summary / Consumer Event
 * Details flow. Alarms and Events use the same component against their own endpoint base.
 */
export function EventClassificationView({
  endpointBase,
  filenamePrefix,
  fixedParams,
  extraFilters,
}: {
  endpointBase: string;
  filenamePrefix: string;
  /** Query params (e.g. orgUnitId from the hierarchy filter) baked into both the classification
   * summary and the drill-down list, so a Region/Zone/Circle/Division/Sub-Division selection
   * scopes this whole view, not just the occurrence table. */
  fixedParams?: Record<string, string>;
  extraFilters?: React.ReactNode;
}) {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<{ classification: string; eventType: string } | null>(null);

  const summaryQueryString = fixedParams && Object.keys(fixedParams).length > 0 ? `?${new URLSearchParams(fixedParams).toString()}` : "";
  const summaryQuery = useQuery({
    queryKey: ["report", `${endpointBase}/classification-summary`, fixedParams],
    queryFn: () => apiClient.get<ClassificationSummaryResponse>(`${endpointBase}/classification-summary${summaryQueryString}`),
    enabled: !selected,
  });

  const acknowledge = useMutation({
    mutationFn: (eventId: string) => apiClient.post(`/api/v1/meter-data/events/${eventId}/acknowledge`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["report", endpointBase] }),
  });
  const resolve = useMutation({
    mutationFn: (eventId: string) => apiClient.post(`/api/v1/meter-data/events/${eventId}/resolve`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["report", endpointBase] }),
  });

  if (selected) {
    const columns: ReportColumn<MeterEventRow>[] = [
      { key: "meterNumber", label: "Meter Number", render: (r) => r.meterNumber },
      { key: "occurred", label: "Occurrence Time", render: (r) => new Date(r.occurredAtUtc).toLocaleString() },
      { key: "resolved", label: "Resolution Time", render: (r) => (r.resolvedAtUtc ? new Date(r.resolvedAtUtc).toLocaleString() : "—") },
      { key: "current", label: "Occ Current", align: "right", render: (r) => r.occCurrent ?? "—" },
      { key: "voltage", label: "Occ Voltage", align: "right", render: (r) => r.occVoltage ?? "—" },
      { key: "kwh", label: "Occ kWh", align: "right", render: (r) => r.occKwh ?? "—" },
      { key: "temp", label: "Occ Temp", align: "right", render: (r) => r.occTemperature ?? "—" },
      { key: "duration", label: "Duration (min)", align: "right", render: (r) => r.durationMinutes ?? "—" },
      { key: "status", label: "Status", render: (r) => <StatusBadge value={r.isAcknowledged ? "Acknowledged" : "Open"} /> },
      {
        key: "action",
        label: "Action",
        align: "right",
        render: (r) => (
          <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
            {!r.resolvedAtUtc && (
              <Button size="small" onClick={() => resolve.mutate(r.id)}>
                Resolve
              </Button>
            )}
            {!r.isAcknowledged && (
              <Button size="small" onClick={() => acknowledge.mutate(r.id)}>
                Acknowledge
              </Button>
            )}
          </Stack>
        ),
      },
    ];

    return (
      <Box>
        <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 2 }}>
          <IconButton size="small" onClick={() => setSelected(null)}>
            <ArrowBackOutlinedIcon fontSize="small" />
          </IconButton>
          <Box>
            <Typography variant="h6">{selected.eventType}</Typography>
            <Typography variant="caption" color="text.secondary">
              Classification: {selected.classification}
            </Typography>
          </Box>
        </Stack>
        <MeterDataListTab<MeterEventRow>
          title={selected.eventType}
          endpoint={endpointBase}
          fixedParams={{ eventType: selected.eventType, ...fixedParams }}
          filenamePrefix={`${filenamePrefix}_${selected.eventType}`}
          columns={columns}
          rowKey={(r) => r.id}
          extraFilters={extraFilters}
        />
      </Box>
    );
  }

  const totals = summaryQuery.data?.totalsByClassification ?? {};

  return (
    <Box>
      {extraFilters && (
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ flexWrap: "wrap", mb: 2 }}>
          {extraFilters}
        </Stack>
      )}
      <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1.5 }}>
        Event Count by Classification
      </Typography>
      <Grid container spacing={2} sx={{ mb: 3 }}>
        {CLASSIFICATIONS.map((c) => {
          const Icon = c.icon;
          const count = totals[c.key] ?? 0;
          return (
            <Grid key={c.key} size={{ xs: 6, sm: 4, md: 2 }}>
              <Card variant="outlined" sx={{ height: "100%" }}>
                <CardContent>
                  <Box sx={{ display: "flex", alignItems: "center", justifyContent: "center", width: 32, height: 32, borderRadius: "var(--radius-md)", bgcolor: c.bg, color: c.color, mb: 1 }}>
                    <Icon fontSize="small" />
                  </Box>
                  <Typography variant="caption" sx={{ color: "var(--card-muted)", display: "block" }}>
                    {c.label}
                  </Typography>
                  <Typography sx={{ fontSize: 22, fontWeight: 700, color: "var(--card-metric)" }}>{count}</Typography>
                </CardContent>
              </Card>
            </Grid>
          );
        })}
      </Grid>

      <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1.5 }}>
        Event Summary
      </Typography>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>#</TableCell>
            <TableCell>Classification</TableCell>
            <TableCell>Event Name</TableCell>
            <TableCell align="right">Event Count</TableCell>
            <TableCell>Last Occurrence</TableCell>
            <TableCell align="right">Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {summaryQuery.data?.summary.length === 0 && (
            <TableRow>
              <TableCell colSpan={6}>No events or alarms found.</TableCell>
            </TableRow>
          )}
          {summaryQuery.data?.summary.map((row, i) => (
            <TableRow key={`${row.classification}-${row.eventType}`} hover>
              <TableCell>{i + 1}</TableCell>
              <TableCell>{row.classification}</TableCell>
              <TableCell>{row.eventType}</TableCell>
              <TableCell align="right">{row.count}</TableCell>
              <TableCell>{new Date(row.lastOccurrenceUtc).toLocaleString()}</TableCell>
              <TableCell align="right">
                <Button size="small" onClick={() => setSelected({ classification: row.classification, eventType: row.eventType })}>
                  View Details →
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Box>
  );
}
