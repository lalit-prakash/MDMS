"use client";

import { useState } from "react";
import { Box, Button, Chip, Menu, MenuItem, Paper, Stack, TextField, Typography, Checkbox, ListItemText } from "@mui/material";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import FilterAltOutlinedIcon from "@mui/icons-material/FilterAltOutlined";
import ViewColumnOutlinedIcon from "@mui/icons-material/ViewColumnOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import { ReportTable, ReportColumn } from "./ReportTable";
import { HierarchyFilter } from "./HierarchyFilter";
import { useReportQuery } from "@/lib/reports/useReportQuery";
import { downloadCsv } from "@/lib/reports/downloadCsv";

/**
 * Shared shell for a network master-data listing (Consumer / DTR / Feeder): a search box + the
 * real Zone→Section hierarchy filter, a column-visibility picker, a record-count title, a
 * server-paginated table, and a Download All (CSV) button — the master-data counterpart of
 * MeterDataListTab, minus the from/to date fields that don't apply to master data.
 */
export function MasterDataListView<TRow>({
  title,
  endpoint,
  filenamePrefix,
  columns,
  rowKey,
  searchPlaceholder,
}: {
  title: string;
  endpoint: string;
  filenamePrefix: string;
  columns: ReportColumn<TRow>[];
  rowKey: (row: TRow) => string;
  searchPlaceholder: string;
}) {
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [orgUnitDraft, setOrgUnitDraft] = useState("");
  const [orgUnitId, setOrgUnitId] = useState("");
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(new Set());
  const [columnMenuAnchor, setColumnMenuAnchor] = useState<HTMLElement | null>(null);

  const params = new URLSearchParams();
  if (search) params.set("search", search);
  if (orgUnitId) params.set("orgUnitId", orgUnitId);
  const filterQuery = params.toString() ? `?${params.toString()}` : "";

  const { data, isLoading, isError, setPage, setPageSize, fullPath } = useReportQuery<TRow, undefined>(endpoint, filterQuery);

  const visibleColumns = columns.filter((c) => !hiddenColumns.has(c.key));

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
                setSearchDraft("");
                setSearch("");
                setOrgUnitDraft("");
                setOrgUnitId("");
              }}
            >
              Clear All
            </Button>
            <Button
              size="small"
              variant="contained"
              onClick={() => {
                setSearch(searchDraft);
                setOrgUnitId(orgUnitDraft);
              }}
            >
              Apply Filters
            </Button>
          </Stack>
        </Stack>

        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ flexWrap: "wrap" }}>
          <TextField
            size="small"
            label="Search"
            placeholder={searchPlaceholder}
            value={searchDraft}
            onChange={(e) => setSearchDraft(e.target.value)}
            sx={{ minWidth: 220 }}
          />
          <HierarchyFilter value={orgUnitDraft} onChange={setOrgUnitDraft} />
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
