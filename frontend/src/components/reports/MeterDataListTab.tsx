"use client";

import { useState } from "react";
import { Box, Button, Stack, TextField } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import { ReportTable, ReportColumn } from "./ReportTable";
import { useReportQuery } from "@/lib/reports/useReportQuery";
import { downloadCsv } from "@/lib/reports/downloadCsv";

/**
 * Shared shell for a meter-data listing tab (LS/DP/IP/BP/Events/Alarms): a Meter ID + From/To
 * date filter row, a server-paginated table (max 100 rows/page, per the reporting spec applied
 * here to raw meter data too), and a Download CSV button that exports every filtered row, not
 * just the current page.
 */
export function MeterDataListTab<TRow>({
  endpoint,
  filenamePrefix,
  columns,
  rowKey,
  dateFieldType = "datetime-local",
  extraFilters,
}: {
  endpoint: string;
  filenamePrefix: string;
  columns: ReportColumn<TRow>[];
  rowKey: (row: TRow) => string;
  dateFieldType?: "date" | "datetime-local";
  extraFilters?: React.ReactNode;
}) {
  const [meterId, setMeterId] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");

  const params = new URLSearchParams();
  if (meterId) params.set("meterId", meterId);
  if (fromDate) params.set("fromDate", dateFieldType === "date" ? fromDate : `${fromDate}:00Z`);
  if (toDate) params.set("toDate", dateFieldType === "date" ? toDate : `${toDate}:00Z`);
  const filterQuery = params.toString() ? `?${params.toString()}` : "";

  const { data, isLoading, isError, setPage, setPageSize, fullPath } = useReportQuery<TRow, undefined>(endpoint, filterQuery);

  return (
    <Box>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ mb: 2, alignItems: { sm: "center" }, flexWrap: "wrap" }}>
        <TextField size="small" label="Meter ID" value={meterId} onChange={(e) => setMeterId(e.target.value)} sx={{ minWidth: 220 }} />
        <TextField
          size="small"
          label="From"
          type={dateFieldType}
          value={fromDate}
          onChange={(e) => setFromDate(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          size="small"
          label="To"
          type={dateFieldType}
          value={toDate}
          onChange={(e) => setToDate(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        {extraFilters}
        <Box sx={{ flex: 1 }} />
        <Button
          variant="outlined"
          size="small"
          startIcon={<DownloadOutlinedIcon fontSize="small" />}
          onClick={() => downloadCsv(fullPath.replace(/[?&](page|pageSize)=\d+/g, ""), `${filenamePrefix}.csv`)}
        >
          Download All (CSV)
        </Button>
      </Stack>

      <ReportTable
        columns={columns}
        rows={data?.data ?? []}
        rowKey={rowKey}
        pagination={data?.pagination}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
        loading={isLoading}
        error={isError}
      />
    </Box>
  );
}
