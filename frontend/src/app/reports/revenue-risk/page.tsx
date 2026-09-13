"use client";

import { useState } from "react";
import { Box, Button, MenuItem, Stack, TextField, Typography } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import ShieldOutlinedIcon from "@mui/icons-material/ShieldOutlined";
import { PageHeader } from "@/components/PageHeader";
import { ReportSummaryCards } from "@/components/reports/ReportSummaryCards";
import { ReportTable, ReportColumn } from "@/components/reports/ReportTable";
import { useReportQuery } from "@/lib/reports/useReportQuery";
import { downloadCsv } from "@/lib/reports/downloadCsv";
import { StatusBadge } from "@/components/StatusBadge";

interface RevenueRiskRow {
  id: string;
  consumerAccountNumber: string | null;
  consumerName: string | null;
  meterSerialNumber: string | null;
  riskScore: number;
  status: string;
  recoveryAmount: number | null;
}
interface RevenueRiskSummary {
  total: number;
  open: number;
  closed: number;
  averageRiskScore: number;
  totalRecovered: number;
}

const ENDPOINT = "/api/v1/reports/revenue-risk";
const STATUSES = ["Detected", "Scored", "Reviewed", "Assigned", "FieldInvestigation", "FindingRecorded", "ActionTaken", "RecoveryRecorded", "Closed"];

export default function RevenueRiskReportPage() {
  const [status, setStatus] = useState("");
  const [minRiskScore, setMinRiskScore] = useState("");

  const params = new URLSearchParams();
  if (status) params.set("status", status);
  if (minRiskScore) params.set("minRiskScore", minRiskScore);
  const filterQuery = params.toString() ? `?${params.toString()}` : "";

  const { data, isLoading, isError, setPage, setPageSize, fullPath } =
    useReportQuery<RevenueRiskRow, RevenueRiskSummary>(ENDPOINT, filterQuery);

  const columns: ReportColumn<RevenueRiskRow>[] = [
    { key: "consumer", label: "Consumer", render: (r) => r.consumerAccountNumber ?? "—" },
    { key: "name", label: "Name", render: (r) => r.consumerName ?? "—" },
    { key: "meter", label: "Meter", render: (r) => r.meterSerialNumber ?? "—" },
    { key: "score", label: "Risk Score", align: "right", render: (r) => r.riskScore },
    { key: "status", label: "Status", render: (r) => <StatusBadge value={r.status} /> },
    { key: "recovery", label: "Recovery Amount", align: "right", render: (r) => (r.recoveryAmount !== null ? `₹${r.recoveryAmount.toLocaleString()}` : "—") },
  ];

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start", mb: 1 }}>
        <PageHeader icon={<ShieldOutlinedIcon fontSize="small" />} title="Revenue Risk" />
        <Button
          variant="outlined"
          size="small"
          startIcon={<DownloadOutlinedIcon fontSize="small" />}
          onClick={() => downloadCsv(fullPath.replace(/[?&](page|pageSize)=\d+/g, ""), "MDMS_Revenue_Risk.csv")}
        >
          Download CSV
        </Button>
      </Stack>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Scored revenue-protection leads and their investigation/recovery status.
      </Typography>

      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ mb: 3 }}>
        <TextField size="small" select label="Status" value={status} onChange={(e) => setStatus(e.target.value)} sx={{ minWidth: 200 }}>
          <MenuItem value="">All</MenuItem>
          {STATUSES.map((s) => (
            <MenuItem key={s} value={s}>{s}</MenuItem>
          ))}
        </TextField>
        <TextField size="small" label="Min Risk Score" type="number" value={minRiskScore} onChange={(e) => setMinRiskScore(e.target.value)} sx={{ minWidth: 160 }} />
      </Stack>

      {data && (
        <ReportSummaryCards
          cards={[
            { label: "Total Leads", value: data.summary.total.toLocaleString() },
            { label: "Open", value: data.summary.open.toLocaleString() },
            { label: "Closed", value: data.summary.closed.toLocaleString() },
            { label: "Avg. Risk Score", value: data.summary.averageRiskScore.toString() },
            { label: "Total Recovered", value: `₹${data.summary.totalRecovered.toLocaleString()}` },
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
