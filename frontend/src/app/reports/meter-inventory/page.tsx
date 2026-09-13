"use client";

import { useState } from "react";
import { Box, Button, MenuItem, Stack, TextField, Typography } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import ElectricMeterOutlinedIcon from "@mui/icons-material/ElectricMeterOutlined";
import { PageHeader } from "@/components/PageHeader";
import { ReportSummaryCards } from "@/components/reports/ReportSummaryCards";
import { ReportTable, ReportColumn } from "@/components/reports/ReportTable";
import { useReportQuery } from "@/lib/reports/useReportQuery";
import { downloadCsv } from "@/lib/reports/downloadCsv";
import { StatusBadge } from "@/components/StatusBadge";

interface MeterInventoryRow {
  meterId: string;
  serialNumber: string;
  phase: string;
  status: string;
  customerAccountNumber: string | null;
  customerName: string | null;
  servicePointAddress: string | null;
  distributionTransformer: string | null;
  installedAtUtc: string | null;
}
interface MeterInventorySummary {
  total: number;
  inStock: number;
  installed: number;
  removed: number;
  retired: number;
}

const ENDPOINT = "/api/v1/reports/meter-inventory";

export default function MeterInventoryReportPage() {
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [phase, setPhase] = useState("");

  const params = new URLSearchParams();
  if (search) params.set("search", search);
  if (status) params.set("status", status);
  if (phase) params.set("phase", phase);
  const filterQuery = params.toString() ? `?${params.toString()}` : "";

  const { data, isLoading, isError, setPage, setPageSize, fullPath } =
    useReportQuery<MeterInventoryRow, MeterInventorySummary>(ENDPOINT, filterQuery);

  const columns: ReportColumn<MeterInventoryRow>[] = [
    { key: "serialNumber", label: "Meter", render: (r) => r.serialNumber },
    { key: "phase", label: "Phase", render: (r) => r.phase },
    { key: "status", label: "Status", render: (r) => <StatusBadge value={r.status} /> },
    { key: "consumer", label: "Consumer", render: (r) => r.customerAccountNumber ?? "—" },
    { key: "consumerName", label: "Consumer Name", render: (r) => r.customerName ?? "—" },
    { key: "servicePoint", label: "Service Point", render: (r) => r.servicePointAddress ?? "—" },
    { key: "dt", label: "Distribution Transformer", render: (r) => r.distributionTransformer ?? "—" },
    { key: "installed", label: "Installed", render: (r) => (r.installedAtUtc ? new Date(r.installedAtUtc).toLocaleDateString() : "—") },
  ];

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start", mb: 1 }}>
        <PageHeader icon={<ElectricMeterOutlinedIcon fontSize="small" />} title="Meter Inventory" />
        <Button
          variant="outlined"
          size="small"
          startIcon={<DownloadOutlinedIcon fontSize="small" />}
          onClick={() => downloadCsv(fullPath.replace(/[?&](page|pageSize)=\d+/g, ""), "MDMS_Meter_Inventory.csv")}
        >
          Download CSV
        </Button>
      </Stack>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Current meter master data joined with consumer, service point, and DT mapping — one row per meter, not per historical installation.
      </Typography>

      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ mb: 3 }}>
        <TextField
          size="small"
          label="Search"
          placeholder="Meter serial or consumer number"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ minWidth: 240 }}
        />
        <TextField size="small" select label="Status" value={status} onChange={(e) => setStatus(e.target.value)} sx={{ minWidth: 160 }}>
          <MenuItem value="">All</MenuItem>
          {["InStock", "Installed", "Removed", "Retired"].map((s) => (
            <MenuItem key={s} value={s}>{s}</MenuItem>
          ))}
        </TextField>
        <TextField size="small" select label="Phase" value={phase} onChange={(e) => setPhase(e.target.value)} sx={{ minWidth: 140 }}>
          <MenuItem value="">All</MenuItem>
          <MenuItem value="Single">Single</MenuItem>
          <MenuItem value="Three">Three</MenuItem>
        </TextField>
      </Stack>

      {data && (
        <ReportSummaryCards
          cards={[
            { label: "Total", value: data.summary.total.toLocaleString() },
            { label: "In Stock", value: data.summary.inStock.toLocaleString() },
            { label: "Installed", value: data.summary.installed.toLocaleString() },
            { label: "Removed", value: data.summary.removed.toLocaleString() },
            { label: "Retired", value: data.summary.retired.toLocaleString() },
          ]}
        />
      )}

      <ReportTable
        columns={columns}
        rows={data?.data ?? []}
        rowKey={(r) => r.meterId}
        pagination={data?.pagination}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        loading={isLoading}
        error={isError}
      />
    </Box>
  );
}
