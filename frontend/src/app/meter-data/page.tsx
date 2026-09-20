"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Box, Button, Tabs, Tab } from "@mui/material";
import { useState } from "react";
import { apiClient } from "@/lib/apiClient";
import { StatusBadge } from "@/components/StatusBadge";
import { ReportColumn } from "@/components/reports/ReportTable";
import { MeterDataListTab } from "@/components/reports/MeterDataListTab";
import { EventClassificationView } from "@/components/reports/EventClassificationView";
import { HierarchyFilter } from "@/components/reports/HierarchyFilter";

import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import { PageHeader } from "@/components/PageHeader";

interface LoadSurveyRow {
  id: string;
  meterId: string;
  meterNumber: string;
  intervalStartUtc: string;
  intervalEndUtc: string;
  mdmEntryTimestampUtc: string;
  cumulativeReading: number;
  consumptionKwh: number;
  averageVoltage: number | null;
  averageCurrent: number | null;
  cumulativeKvahImport: number | null;
  cumulativeKwhExport: number | null;
  cumulativeKvahExport: number | null;
  quality: string;
  source: string;
}

interface DailyLoadProfileRow {
  id: string;
  meterId: string;
  meterNumber: string;
  profileDate: string;
  mdmEntryTimestampUtc: string;
  consumptionKwh: number;
  kvahImport: number | null;
  kwhExport: number | null;
  kvahExport: number | null;
  quality: string;
  source: string;
}

interface InstantaneousProfile {
  id: string;
  meterId: string;
  meterNumber: string;
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
  meterNumber: string;
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
  meterNumber: string;
  occurredAtUtc: string;
  eventType: string;
  severity: string;
  description: string | null;
  isAcknowledged: boolean;
}

type DataTab = "ls" | "dlp" | "ip" | "bp" | "events" | "alarms";

const TABS: { value: DataTab; label: string }[] = [
  { value: "ls", label: "Load Survey (LS)" },
  { value: "dlp", label: "Daily Profile (DP)" },
  { value: "ip", label: "Instantaneous Profile (IP)" },
  { value: "bp", label: "Billing Profile (BP)" },
  { value: "events", label: "Events" },
  { value: "alarms", label: "Alarms" },
];

const lsColumns: ReportColumn<LoadSurveyRow>[] = [
  { key: "meterNumber", label: "Meter Number", render: (r) => r.meterNumber },
  { key: "meterTimestamp", label: "Meter Timestamp", render: (r) => new Date(r.intervalEndUtc).toLocaleString() },
  { key: "mdmEntryTimestamp", label: "MDM Entry Timestamp", render: (r) => new Date(r.mdmEntryTimestampUtc).toLocaleString() },
  { key: "avgVoltage", label: "Average Voltage", align: "right", render: (r) => r.averageVoltage ?? "—" },
  { key: "avgCurrent", label: "Average Current", align: "right", render: (r) => r.averageCurrent ?? "—" },
  { key: "cumKwhImport", label: "Cumulative Energy kWh Import", align: "right", render: (r) => r.cumulativeReading },
  { key: "cumKvahImport", label: "Cumulative Energy kVAh Import", align: "right", render: (r) => r.cumulativeKvahImport ?? "—" },
  { key: "cumKwhExport", label: "Cumulative Energy kWh Export", align: "right", render: (r) => r.cumulativeKwhExport ?? "—" },
  { key: "cumKvahExport", label: "Cumulative Energy kVAh Export", align: "right", render: (r) => r.cumulativeKvahExport ?? "—" },
  { key: "quality", label: "Quality", render: (r) => <StatusBadge value={r.quality} /> },
  { key: "source", label: "Source", render: (r) => <StatusBadge value={r.source} /> },
];

const dlpColumns: ReportColumn<DailyLoadProfileRow>[] = [
  { key: "meterNumber", label: "Meter Number", render: (r) => r.meterNumber },
  { key: "date", label: "Profile Date", render: (r) => r.profileDate },
  { key: "mdmEntryTimestamp", label: "MDM Entry Timestamp", render: (r) => new Date(r.mdmEntryTimestampUtc).toLocaleString() },
  { key: "kwhImport", label: "kWh Import", align: "right", render: (r) => r.consumptionKwh },
  { key: "kvahImport", label: "kVAh Import", align: "right", render: (r) => r.kvahImport ?? "—" },
  { key: "kwhExport", label: "kWh Export", align: "right", render: (r) => r.kwhExport ?? "—" },
  { key: "kvahExport", label: "kVAh Export", align: "right", render: (r) => r.kvahExport ?? "—" },
  { key: "quality", label: "Quality", render: (r) => <StatusBadge value={r.quality} /> },
  { key: "source", label: "Source", render: (r) => <StatusBadge value={r.source} /> },
];

const ipColumns: ReportColumn<InstantaneousProfile>[] = [
  { key: "meterNumber", label: "Meter Number", render: (r) => r.meterNumber },
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
  { key: "meterNumber", label: "Meter Number", render: (r) => r.meterNumber },
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
    { key: "meterNumber", label: "Meter Number", render: (r) => r.meterNumber },
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
  const [tab, setTab] = useState<DataTab>("ls");
  const [orgUnitId, setOrgUnitId] = useState("");

  const acknowledgeAlarms = useAcknowledge("/api/v1/meter-data/alarms");

  const hierarchyFilter = <HierarchyFilter value={orgUnitId} onChange={setOrgUnitId} />;
  const hierarchyParams = orgUnitId ? { orgUnitId } : undefined;
  const savedFilterProps = {
    extraFilters: hierarchyFilter,
    extraValues: (orgUnitId ? { orgUnitId } : {}) as Record<string, string>,
    onExtraValuesLoad: (v: Record<string, string>) => setOrgUnitId(v.orgUnitId ?? ""),
  };

  return (
    <Box>
      <PageHeader icon={<StorageOutlinedIcon fontSize="small" />} title="Meter Data" />

      <Tabs value={tab} onChange={(_e, v) => setTab(v)} sx={{ mb: 3, borderBottom: "1px solid var(--color-border-default)" }} variant="scrollable">
        {TABS.map((t) => (
          <Tab key={t.value} value={t.value} label={t.label} sx={{ textTransform: "none", fontSize: 13 }} />
        ))}
      </Tabs>

      {tab === "ls" && (
        <MeterDataListTab<LoadSurveyRow>
          title="Load Survey (LS)"
          endpoint="/api/v1/meter-data/ls"
          fixedParams={hierarchyParams}
          filenamePrefix="MDMS_LoadSurvey"
          columns={lsColumns}
          rowKey={(r) => r.id}
          {...savedFilterProps}
        />
      )}

      {tab === "dlp" && (
        <MeterDataListTab<DailyLoadProfileRow>
          title="Daily Profile (DP)"
          endpoint="/api/v1/meter-data/dlp"
          fixedParams={hierarchyParams}
          filenamePrefix="MDMS_DailyProfile"
          columns={dlpColumns}
          rowKey={(r) => r.id}
          dateFieldType="date"
          {...savedFilterProps}
        />
      )}

      {tab === "ip" && (
        <MeterDataListTab<InstantaneousProfile>
          title="Instantaneous Profile (IP)"
          endpoint="/api/v1/meter-data/ip"
          fixedParams={hierarchyParams}
          filenamePrefix="MDMS_InstantaneousProfile"
          columns={ipColumns}
          rowKey={(r) => r.id}
          {...savedFilterProps}
        />
      )}

      {tab === "bp" && (
        <MeterDataListTab<BillingProfile>
          title="Billing Profile (BP)"
          endpoint="/api/v1/meter-data/bp"
          fixedParams={hierarchyParams}
          filenamePrefix="MDMS_BillingProfile"
          columns={bpColumns}
          rowKey={(r) => r.id}
          dateFieldType="date"
          {...savedFilterProps}
        />
      )}

      {tab === "events" && (
        <EventClassificationView endpointBase="/api/v1/meter-data/events" filenamePrefix="MDMS_Events" fixedParams={hierarchyParams} extraFilters={hierarchyFilter} />
      )}

      {tab === "alarms" && (
        <MeterDataListTab<MeterEvent>
          title="Alarm Details"
          endpoint="/api/v1/meter-data/alarms"
          fixedParams={hierarchyParams}
          filenamePrefix="MDMS_Alarms"
          columns={eventColumns(acknowledgeAlarms)}
          rowKey={(r) => r.id}
          {...savedFilterProps}
        />
      )}
    </Box>
  );
}
