"use client";

import { useState } from "react";
import { Box, Button, MenuItem, Stack, TextField, Typography } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import { PageHeader } from "@/components/PageHeader";
import { ReportSummaryCards } from "@/components/reports/ReportSummaryCards";
import { ReportTable, ReportColumn } from "@/components/reports/ReportTable";
import { useReportQuery } from "@/lib/reports/useReportQuery";
import { downloadCsv } from "@/lib/reports/downloadCsv";
import { StatusBadge } from "@/components/StatusBadge";

interface VeeExecutionRow {
  id: string;
  ruleName: string;
  measurementType: string;
  meterSerialNumber: string;
  slotStartUtc: string;
  slotEndUtc: string;
  resultQuality: string;
  newValue: number | null;
  details: string;
}
interface VeeExecutionSummary {
  total: number;
  passed: number;
  failed: number;
  passRatePercent: number;
}

const ENDPOINT = "/api/v1/reports/vee-execution";
const QUALITIES = ["Valid", "NegativeConsumption", "OutOfRange", "Missing", "Suspect"];

export default function VeeExecutionReportPage() {
  const [meter, setMeter] = useState("");
  const [quality, setQuality] = useState("");

  const params = new URLSearchParams();
  if (meter) params.set("meterSerialNumber", meter);
  if (quality) params.set("resultQuality", quality);
  const filterQuery = params.toString() ? `?${params.toString()}` : "";

  const { data, isLoading, isError, setPage, setPageSize, fullPath } =
    useReportQuery<VeeExecutionRow, VeeExecutionSummary>(ENDPOINT, filterQuery);

  const columns: ReportColumn<VeeExecutionRow>[] = [
    { key: "meter", label: "Meter", render: (r) => r.meterSerialNumber },
    { key: "rule", label: "Rule", render: (r) => r.ruleName },
    { key: "type", label: "Measurement Type", render: (r) => r.measurementType },
    { key: "slot", label: "Slot Start", render: (r) => new Date(r.slotStartUtc).toLocaleString() },
    { key: "quality", label: "Result Quality", render: (r) => <StatusBadge value={r.resultQuality} /> },
    { key: "value", label: "New Value", align: "right", render: (r) => r.newValue ?? "—" },
    { key: "details", label: "Details", render: (r) => r.details },
  ];

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start", mb: 1 }}>
        <PageHeader icon={<FactCheckOutlinedIcon fontSize="small" />} title="VEE Execution" />
        <Button
          variant="outlined"
          size="small"
          startIcon={<DownloadOutlinedIcon fontSize="small" />}
          onClick={() => downloadCsv(fullPath.replace(/[?&](page|pageSize)=\d+/g, ""), "MDMS_VEE_Execution.csv")}
        >
          Download CSV
        </Button>
      </Stack>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Every validation/estimation/editing run and its resulting data quality — the authoritative VEE result, consumed as-is (never re-derived).
      </Typography>

      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ mb: 3 }}>
        <TextField size="small" label="Meter" placeholder="Serial number" value={meter} onChange={(e) => setMeter(e.target.value)} sx={{ minWidth: 220 }} />
        <TextField size="small" select label="Result Quality" value={quality} onChange={(e) => setQuality(e.target.value)} sx={{ minWidth: 180 }}>
          <MenuItem value="">All</MenuItem>
          {QUALITIES.map((q) => (
            <MenuItem key={q} value={q}>{q}</MenuItem>
          ))}
        </TextField>
      </Stack>

      {data && (
        <ReportSummaryCards
          cards={[
            { label: "Total", value: data.summary.total.toLocaleString() },
            { label: "Passed", value: data.summary.passed.toLocaleString() },
            { label: "Failed", value: data.summary.failed.toLocaleString() },
            { label: "Pass Rate", value: `${data.summary.passRatePercent}%` },
          ]}
        />
      )}

      <ReportTable
        columns={columns}
        rows={data?.data ?? []}
        rowKey={(r) => r.id}
        pagination={data?.pagination}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        loading={isLoading}
        error={isError}
      />
    </Box>
  );
}
