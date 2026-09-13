"use client";

import { useMemo, useState } from "react";
import { Box, Button, Chip, Menu, MenuItem, Paper, Stack, TextField, Typography, Checkbox, ListItemText } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import FilterAltOutlinedIcon from "@mui/icons-material/FilterAltOutlined";
import ViewColumnOutlinedIcon from "@mui/icons-material/ViewColumnOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import { ReportTable, ReportColumn } from "./ReportTable";
import { useReportQuery } from "@/lib/reports/useReportQuery";
import { downloadCsv } from "@/lib/reports/downloadCsv";

interface AppliedFilters {
  meterId: string;
  fromDate: string;
  toDate: string;
}

const EMPTY_FILTERS: AppliedFilters = { meterId: "", fromDate: "", toDate: "" };

/**
 * Shared shell for a meter-data listing tab (LS/DP/IP/BP/Events/Alarms): a Filters panel
 * (Meter ID + From/To date, applied on demand via Apply Filters / Clear All — not live-as-you-
 * type, matching the reference UI), a column-visibility picker, a record-count title, a
 * server-paginated table (max 100 rows/page, numbered pagination), and a Download All (CSV)
 * button that exports every filtered row, not just the current page.
 */
export function MeterDataListTab<TRow>({
  title,
  endpoint,
  filenamePrefix,
  columns,
  rowKey,
  dateFieldType = "datetime-local",
  extraFilters,
}: {
  title: string;
  endpoint: string;
  filenamePrefix: string;
  columns: ReportColumn<TRow>[];
  rowKey: (row: TRow) => string;
  dateFieldType?: "date" | "datetime-local";
  extraFilters?: React.ReactNode;
}) {
  const [draft, setDraft] = useState<AppliedFilters>(EMPTY_FILTERS);
  const [applied, setApplied] = useState<AppliedFilters>(EMPTY_FILTERS);
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(new Set());
  const [columnMenuAnchor, setColumnMenuAnchor] = useState<HTMLElement | null>(null);

  const params = new URLSearchParams();
  if (applied.meterId) params.set("meterId", applied.meterId);
  if (applied.fromDate) params.set("fromDate", dateFieldType === "date" ? applied.fromDate : `${applied.fromDate}:00Z`);
  if (applied.toDate) params.set("toDate", dateFieldType === "date" ? applied.toDate : `${applied.toDate}:00Z`);
  const filterQuery = params.toString() ? `?${params.toString()}` : "";

  const { data, isLoading, isError, setPage, setPageSize, fullPath } = useReportQuery<TRow, undefined>(endpoint, filterQuery);

  const visibleColumns = useMemo(() => columns.filter((c) => !hiddenColumns.has(c.key)), [columns, hiddenColumns]);

  return (
    <Box>
      <Paper variant="outlined" sx={{ p: 2.5, mb: 3 }}>
        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", mb: 2 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
            <FilterAltOutlinedIcon fontSize="small" sx={{ color: "var(--color-accent)" }} />
            <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
              Filters
            </Typography>
          </Stack>
          <Stack direction="row" spacing={1}>
            <Button
              size="small"
              startIcon={<RefreshOutlinedIcon fontSize="small" />}
              onClick={() => {
                setDraft(EMPTY_FILTERS);
                setApplied(EMPTY_FILTERS);
              }}
            >
              Clear All
            </Button>
            <Button size="small" variant="contained" onClick={() => setApplied(draft)}>
              Apply Filters
            </Button>
          </Stack>
        </Stack>

        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ flexWrap: "wrap" }}>
          <TextField
            size="small"
            label="Meter ID"
            value={draft.meterId}
            onChange={(e) => setDraft((d) => ({ ...d, meterId: e.target.value }))}
            sx={{ minWidth: 220 }}
          />
          <TextField
            size="small"
            label="From Date"
            type={dateFieldType}
            value={draft.fromDate}
            onChange={(e) => setDraft((d) => ({ ...d, fromDate: e.target.value }))}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            size="small"
            label="To Date"
            type={dateFieldType}
            value={draft.toDate}
            onChange={(e) => setDraft((d) => ({ ...d, toDate: e.target.value }))}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          {extraFilters}
        </Stack>
      </Paper>

      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", mb: 1.5, flexWrap: "wrap", gap: 1 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
          <Typography variant="h6">{title}</Typography>
          {data && <Chip size="small" label={`${data.pagination.totalRecords.toLocaleString()} Records`} />}
        </Stack>
        <Stack direction="row" spacing={1}>
          <Button
            size="small"
            variant="outlined"
            startIcon={<ViewColumnOutlinedIcon fontSize="small" />}
            onClick={(e) => setColumnMenuAnchor(e.currentTarget)}
          >
            Choose Columns
          </Button>
          <Button
            variant="outlined"
            size="small"
            startIcon={<DownloadOutlinedIcon fontSize="small" />}
            onClick={() => downloadCsv(fullPath.replace(/[?&](page|pageSize)=\d+/g, ""), `${filenamePrefix}.csv`)}
          >
            Download (CSV)
          </Button>
        </Stack>
      </Stack>

      <Menu anchorEl={columnMenuAnchor} open={!!columnMenuAnchor} onClose={() => setColumnMenuAnchor(null)}>
        {columns.map((c) => (
          <MenuItem
            key={c.key}
            onClick={() =>
              setHiddenColumns((prev) => {
                const next = new Set(prev);
                if (next.has(c.key)) next.delete(c.key);
                else next.add(c.key);
                return next;
              })
            }
            dense
          >
            <Checkbox size="small" checked={!hiddenColumns.has(c.key)} sx={{ p: 0.5, mr: 1 }} />
            <ListItemText primary={c.label} />
          </MenuItem>
        ))}
      </Menu>

      <ReportTable
        columns={visibleColumns}
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
