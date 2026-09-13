"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Typography,
  Paper,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  Stack,
  Button,
  Switch,
  FormControlLabel,
  Tabs,
  Tab,
} from "@mui/material";
import { useState } from "react";
import { apiClient } from "@/lib/apiClient";
import { LoadSurveyInterval, DailyLoadProfile, DataQualityHold } from "@/lib/types";
import { StatusBadge } from "@/components/StatusBadge";
import { ReportColumn } from "@/components/reports/ReportTable";
import { MeterDataListTab } from "@/components/reports/MeterDataListTab";
import { EventClassificationView } from "@/components/reports/EventClassificationView";

import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import { PageHeader } from "@/components/PageHeader";

interface InstantaneousProfile {
  id: string;
  meterId: string;
  meterTimeUtc: string;
  voltage: number;
  phaseCurrent: number;
  powerFactor: number;
  frequency: number;
  kw: number;
  kva: number;
  kwh: number;
  kvah: number;
  loadLimitState: string;
  tamperCount: number;
}

interface BillingProfile {
  id: string;
  meterId: string;
  billingDate: string;
  cumulativeKwhImport: number;
  cumulativeKvahImport: number;
  cumulativeKwhExport: number;
  cumulativeKvahExport: number;
  averagePowerFactor: number;
  kwhByTariffZone: number[];
  kvahByTariffZone: number[];
  maximumDemandKw: number;
  maximumDemandKva: number;
  billingPowerOnDurationMinutes: number;
}

interface MeterEvent {
  id: string;
  meterId: string;
  occurredAtUtc: string;
  eventType: string;
  severity: string;
  description: string | null;
  isAcknowledged: boolean;
}

type DataTab = "ls" | "dlp" | "ip" | "bp" | "events" | "alarms" | "holds";

const TABS: { value: DataTab; label: string }[] = [
  { value: "ls", label: "Load Survey (LS)" },
  { value: "dlp", label: "Daily Profile (DP)" },
  { value: "ip", label: "Instantaneous Profile (IP)" },
  { value: "bp", label: "Billing Profile (BP)" },
  { value: "events", label: "Events" },
  { value: "alarms", label: "Alarms" },
  { value: "holds", label: "Data-Quality Holds" },
];

const lsColumns: ReportColumn<LoadSurveyInterval>[] = [
  { key: "start", label: "Interval Start", render: (r) => new Date(r.intervalStartUtc).toLocaleString() },
  { key: "end", label: "Interval End", render: (r) => new Date(r.intervalEndUtc).toLocaleString() },
  { key: "cumulative", label: "Cumulative", align: "right", render: (r) => r.cumulativeReading },
  { key: "consumption", label: "Consumption (kWh)", align: "right", render: (r) => r.consumptionKwh },
  { key: "quality", label: "Quality", render: (r) => <StatusBadge value={r.quality} /> },
  { key: "source", label: "Source", render: (r) => <StatusBadge value={r.source} /> },
];

const dlpColumns: ReportColumn<DailyLoadProfile>[] = [
  { key: "date", label: "Profile Date", render: (r) => r.profileDate },
  { key: "kwhImport", label: "kWh Import", align: "right", render: (r) => r.consumptionKwh },
  { key: "kvahImport", label: "kVAh Import", align: "right", render: (r) => r.kvahImport ?? "—" },
  { key: "kwhExport", label: "kWh Export", align: "right", render: (r) => r.kwhExport ?? "—" },
  { key: "kvahExport", label: "kVAh Export", align: "right", render: (r) => r.kvahExport ?? "—" },
  { key: "quality", label: "Quality", render: (r) => <StatusBadge value={r.quality} /> },
  { key: "source", label: "Source", render: (r) => <StatusBadge value={r.source} /> },
];

const ipColumns: ReportColumn<InstantaneousProfile>[] = [
  { key: "time", label: "Meter Time", render: (r) => new Date(r.meterTimeUtc).toLocaleString() },
  { key: "voltage", label: "Voltage", align: "right", render: (r) => r.voltage },
  { key: "current", label: "Current", align: "right", render: (r) => r.phaseCurrent },
  { key: "pf", label: "PF", align: "right", render: (r) => r.powerFactor },
  { key: "freq", label: "Freq", align: "right", render: (r) => r.frequency },
  { key: "kw", label: "kW", align: "right", render: (r) => r.kw },
  { key: "kva", label: "kVA", align: "right", render: (r) => r.kva },
  { key: "kwh", label: "kWh", align: "right", render: (r) => r.kwh },
  { key: "kvah", label: "kVAh", align: "right", render: (r) => r.kvah },
  { key: "limit", label: "Load Limit", render: (r) => <StatusBadge value={r.loadLimitState} /> },
  { key: "tamper", label: "Tamper", align: "right", render: (r) => r.tamperCount },
];

const tzHeader = (prefix: "kWh" | "kVAh", zone: number): ReportColumn<BillingProfile> => ({
  key: `${prefix}Tz${zone}`,
  label: `${prefix} TZ${zone}`,
  align: "right",
  render: (r) => (prefix === "kWh" ? r.kwhByTariffZone : r.kvahByTariffZone)[zone - 1] ?? "—",
});

const bpColumns: ReportColumn<BillingProfile>[] = [
  { key: "date", label: "Billing Date", render: (r) => r.billingDate },
  { key: "cumKwhImport", label: "Cum. kWh Import", align: "right", render: (r) => r.cumulativeKwhImport },
  { key: "cumKvahImport", label: "Cum. kVAh Import", align: "right", render: (r) => r.cumulativeKvahImport },
  { key: "cumKwhExport", label: "Cum. kWh Export", align: "right", render: (r) => r.cumulativeKwhExport },
  { key: "cumKvahExport", label: "Cum. kVAh Export", align: "right", render: (r) => r.cumulativeKvahExport },
  { key: "avgPf", label: "Avg. PF", align: "right", render: (r) => r.averagePowerFactor },
  ...[1, 2, 3, 4, 5, 6, 7, 8].map((z) => tzHeader("kWh", z)),
  ...[1, 2, 3, 4, 5, 6, 7, 8].map((z) => tzHeader("kVAh", z)),
  { key: "mdKw", label: "Max Demand kW", align: "right", render: (r) => r.maximumDemandKw },
  { key: "mdKva", label: "Max Demand kVA", align: "right", render: (r) => r.maximumDemandKva },
  { key: "powerOn", label: "Power On Duration (min)", align: "right", render: (r) => r.billingPowerOnDurationMinutes },
];

function useAcknowledge(queryKeyPrefix: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (eventId: string) => apiClient.post(`/api/v1/meter-data/events/${eventId}/acknowledge`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["report", queryKeyPrefix] }),
  });
}

function eventColumns(acknowledge: ReturnType<typeof useAcknowledge>): ReportColumn<MeterEvent>[] {
  return [
    { key: "occurred", label: "Occurred", render: (r) => new Date(r.occurredAtUtc).toLocaleString() },
    { key: "type", label: "Type", render: (r) => r.eventType },
    { key: "severity", label: "Severity", render: (r) => <StatusBadge value={r.severity} /> },
    { key: "description", label: "Description", render: (r) => r.description ?? "—" },
    { key: "status", label: "Status", render: (r) => <StatusBadge value={r.isAcknowledged ? "Acknowledged" : "Open"} /> },
    {
      key: "action",
      label: "Action",
      align: "right",
      render: (r) =>
        !r.isAcknowledged && (
          <Button size="small" onClick={() => acknowledge.mutate(r.id)}>
            Acknowledge
          </Button>
        ),
    },
  ];
}

export default function MeterDataPage() {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<DataTab>("ls");
  const [activeOnly, setActiveOnly] = useState(true);

  const holdsQuery = useQuery({
    queryKey: ["billing-holds", activeOnly],
    queryFn: () => apiClient.get<DataQualityHold[]>(`/api/v1/meter-data/billing-holds?activeOnly=${activeOnly}`),
    enabled: tab === "holds",
  });

  const clearHold = useMutation({
    mutationFn: (meterIdToClear: string) =>
      apiClient.post(`/api/v1/meter-data/${meterIdToClear}/billing-hold/clear`, {
        resolutionNote: "Cleared from the MDMS console.",
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["billing-holds"] }),
  });

  const acknowledgeAlarms = useAcknowledge("/api/v1/meter-data/alarms");

  return (
    <Box>
      <PageHeader icon={<StorageOutlinedIcon fontSize="small" />} title="Meter Data" />
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Every category of meter-reported data this system records: 30-minute Load Survey, Daily
        Profile, 15-minute Instantaneous Profile, monthly Billing Profile, and Events and Alarms as
        separate screens. Every list is server-paginated (100 rows/page max) with a Download All CSV.
      </Typography>

      <Tabs value={tab} onChange={(_e, v) => setTab(v)} sx={{ mb: 3, borderBottom: "1px solid var(--color-border-default)" }} variant="scrollable">
        {TABS.map((t) => (
          <Tab key={t.value} value={t.value} label={t.label} sx={{ textTransform: "none", fontSize: 13 }} />
        ))}
      </Tabs>

      {tab === "ls" && (
        <MeterDataListTab<LoadSurveyInterval>
          title="Load Survey (LS)"
          endpoint="/api/v1/meter-data/ls"
          filenamePrefix="MDMS_LoadSurvey"
          columns={lsColumns}
          rowKey={(r) => r.id}
        />
      )}

      {tab === "dlp" && (
        <MeterDataListTab<DailyLoadProfile>
          title="Daily Profile (DP)"
          endpoint="/api/v1/meter-data/dlp"
          filenamePrefix="MDMS_DailyProfile"
          columns={dlpColumns}
          rowKey={(r) => r.id}
          dateFieldType="date"
        />
      )}

      {tab === "ip" && (
        <MeterDataListTab<InstantaneousProfile>
          title="Instantaneous Profile (IP)"
          endpoint="/api/v1/meter-data/ip"
          filenamePrefix="MDMS_InstantaneousProfile"
          columns={ipColumns}
          rowKey={(r) => r.id}
        />
      )}

      {tab === "bp" && (
        <MeterDataListTab<BillingProfile>
          title="Billing Profile (BP)"
          endpoint="/api/v1/meter-data/bp"
          filenamePrefix="MDMS_BillingProfile"
          columns={bpColumns}
          rowKey={(r) => r.id}
          dateFieldType="date"
        />
      )}

      {tab === "events" && (
        <EventClassificationView endpointBase="/api/v1/meter-data/events" filenamePrefix="MDMS_Events" />
      )}

      {tab === "alarms" && (
        <MeterDataListTab<MeterEvent>
          title="Alarm Details"
          endpoint="/api/v1/meter-data/alarms"
          filenamePrefix="MDMS_Alarms"
          columns={eventColumns(acknowledgeAlarms)}
          rowKey={(r) => r.id}
        />
      )}

      {tab === "holds" && (
        <>
          <Stack direction="row" sx={{ alignItems: "center", justifyContent: "flex-end", mb: 1 }}>
            <FormControlLabel
              control={<Switch checked={activeOnly} onChange={(e) => setActiveOnly(e.target.checked)} />}
              label="Active only"
            />
          </Stack>
          <Paper variant="outlined" sx={{ overflowX: "auto" }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Meter ID</TableCell>
                  <TableCell>Reason</TableCell>
                  <TableCell>Raised</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Action</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {holdsQuery.data?.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5}>No holds found.</TableCell>
                  </TableRow>
                )}
                {holdsQuery.data?.map((h) => (
                  <TableRow key={h.id} hover>
                    <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{h.meterId}</TableCell>
                    <TableCell>{h.reason}</TableCell>
                    <TableCell>{new Date(h.raisedAtUtc).toLocaleString()}</TableCell>
                    <TableCell><StatusBadge value={h.isActive ? "Active" : "Cleared"} /></TableCell>
                    <TableCell align="right">
                      {h.isActive && (
                        <Button size="small" onClick={() => clearHold.mutate(h.meterId)}>
                          Clear
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>
        </>
      )}
    </Box>
  );
}
