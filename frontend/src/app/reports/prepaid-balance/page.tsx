"use client";

import { useState } from "react";
import { Box, Button, MenuItem, Stack, TextField, Typography } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import { PageHeader } from "@/components/PageHeader";
import { ReportSummaryCards } from "@/components/reports/ReportSummaryCards";
import { ReportTable, ReportColumn } from "@/components/reports/ReportTable";
import { useReportQuery } from "@/lib/reports/useReportQuery";
import { downloadCsv } from "@/lib/reports/downloadCsv";
import { StatusBadge } from "@/components/StatusBadge";

interface PrepaidBalanceRow {
  accountId: string;
  consumerAccountNumber: string | null;
  consumerName: string | null;
  balance: number;
  isConnected: boolean;
}
interface PrepaidBalanceSummary {
  total: number;
  connected: number;
  disconnected: number;
  totalBalance: number;
}

const ENDPOINT = "/api/v1/reports/prepaid-balance";

export default function PrepaidBalanceReportPage() {
  const [search, setSearch] = useState("");
  const [connected, setConnected] = useState("");

  const params = new URLSearchParams();
  if (search) params.set("search", search);
  if (connected) params.set("isConnected", connected);
  const filterQuery = params.toString() ? `?${params.toString()}` : "";

  const { data, isLoading, isError, setPage, setPageSize, fullPath } =
    useReportQuery<PrepaidBalanceRow, PrepaidBalanceSummary>(ENDPOINT, filterQuery);

  const columns: ReportColumn<PrepaidBalanceRow>[] = [
    { key: "consumer", label: "Consumer", render: (r) => r.consumerAccountNumber ?? "—" },
    { key: "name", label: "Name", render: (r) => r.consumerName ?? "—" },
    { key: "balance", label: "Balance", align: "right", render: (r) => `₹${r.balance.toLocaleString()}` },
    { key: "status", label: "Connection", render: (r) => <StatusBadge value={r.isConnected ? "Connected" : "Disconnected"} /> },
  ];

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start", mb: 1 }}>
        <PageHeader icon={<AccountBalanceWalletOutlinedIcon fontSize="small" />} title="Prepaid Balance" />
        <Button
          variant="outlined"
          size="small"
          startIcon={<DownloadOutlinedIcon fontSize="small" />}
          onClick={() => downloadCsv(fullPath.replace(/[?&](page|pageSize)=\d+/g, ""), "MDMS_Prepaid_Balance.csv")}
        >
          Download CSV
        </Button>
      </Stack>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Current wallet balance and connection status per prepaid account.
      </Typography>

      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ mb: 3 }}>
        <TextField size="small" label="Search" placeholder="Consumer number" value={search} onChange={(e) => setSearch(e.target.value)} sx={{ minWidth: 240 }} />
        <TextField size="small" select label="Connection" value={connected} onChange={(e) => setConnected(e.target.value)} sx={{ minWidth: 160 }}>
          <MenuItem value="">All</MenuItem>
          <MenuItem value="true">Connected</MenuItem>
          <MenuItem value="false">Disconnected</MenuItem>
        </TextField>
      </Stack>

      {data && (
        <ReportSummaryCards
          cards={[
            { label: "Total Accounts", value: data.summary.total.toLocaleString() },
            { label: "Connected", value: data.summary.connected.toLocaleString() },
            { label: "Disconnected", value: data.summary.disconnected.toLocaleString() },
            { label: "Total Balance", value: `₹${data.summary.totalBalance.toLocaleString()}` },
          ]}
        />
      )}

      <ReportTable
        columns={columns}
        rows={data?.data ?? []}
        rowKey={(r) => r.accountId}
        pagination={data?.pagination}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        loading={isLoading}
        error={isError}
      />
    </Box>
  );
}
