"use client";

import { useState } from "react";
import { Box, Button, MenuItem, Stack, TextField, Typography } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import SupportAgentOutlinedIcon from "@mui/icons-material/SupportAgentOutlined";
import { PageHeader } from "@/components/PageHeader";
import { ReportSummaryCards } from "@/components/reports/ReportSummaryCards";
import { ReportTable, ReportColumn } from "@/components/reports/ReportTable";
import { useReportQuery } from "@/lib/reports/useReportQuery";
import { downloadCsv } from "@/lib/reports/downloadCsv";
import { StatusBadge } from "@/components/StatusBadge";

interface ComplaintRow {
  id: string;
  consumerAccountNumber: string | null;
  consumerName: string | null;
  source: string;
  description: string;
  status: string;
  slaDueUtc: string;
  resolvedAtUtc: string | null;
  closedAtUtc: string | null;
}
interface ComplaintSummary {
  total: number;
  open: number;
  resolved: number;
  closed: number;
  slaBreached: number;
}

const ENDPOINT = "/api/v1/reports/complaint-register";
const STATUSES = ["Open", "Assigned", "InProgress", "Resolved", "Closed"];

export default function ComplaintRegisterReportPage() {
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");

  const params = new URLSearchParams();
  if (search) params.set("search", search);
  if (status) params.set("status", status);
  const filterQuery = params.toString() ? `?${params.toString()}` : "";

  const { data, isLoading, isError, setPage, setPageSize, fullPath } =
    useReportQuery<ComplaintRow, ComplaintSummary>(ENDPOINT, filterQuery);

  const columns: ReportColumn<ComplaintRow>[] = [
    { key: "consumer", label: "Consumer", render: (r) => r.consumerAccountNumber ?? "—" },
    { key: "name", label: "Name", render: (r) => r.consumerName ?? "—" },
    { key: "source", label: "Source", render: (r) => r.source },
    { key: "description", label: "Description", render: (r) => r.description },
    { key: "status", label: "Status", render: (r) => <StatusBadge value={r.status} /> },
    { key: "sla", label: "SLA Due", render: (r) => new Date(r.slaDueUtc).toLocaleDateString() },
    { key: "resolved", label: "Resolved", render: (r) => (r.resolvedAtUtc ? new Date(r.resolvedAtUtc).toLocaleDateString() : "—") },
  ];

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start", mb: 1 }}>
        <PageHeader icon={<SupportAgentOutlinedIcon fontSize="small" />} title="Complaint Register" />
        <Button
          variant="outlined"
          size="small"
          startIcon={<DownloadOutlinedIcon fontSize="small" />}
          onClick={() => downloadCsv(fullPath.replace(/[?&](page|pageSize)=\d+/g, ""), "MDMS_Complaint_Register.csv")}
        >
          Download CSV
        </Button>
      </Stack>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Every complaint ticket, its SLA due date, and resolution/closure status.
      </Typography>

      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ mb: 3 }}>
        <TextField size="small" label="Search" placeholder="Description or consumer number" value={search} onChange={(e) => setSearch(e.target.value)} sx={{ minWidth: 260 }} />
        <TextField size="small" select label="Status" value={status} onChange={(e) => setStatus(e.target.value)} sx={{ minWidth: 160 }}>
          <MenuItem value="">All</MenuItem>
          {STATUSES.map((s) => (
            <MenuItem key={s} value={s}>{s}</MenuItem>
          ))}
        </TextField>
      </Stack>

      {data && (
        <ReportSummaryCards
          cards={[
            { label: "Total", value: data.summary.total.toLocaleString() },
            { label: "Open", value: data.summary.open.toLocaleString() },
            { label: "Resolved", value: data.summary.resolved.toLocaleString() },
            { label: "Closed", value: data.summary.closed.toLocaleString() },
            { label: "SLA Breached", value: data.summary.slaBreached.toLocaleString() },
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
