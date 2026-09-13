import {
  Box,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
  IconButton,
  Button,
} from "@mui/material";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { PaginationInfo, REPORT_PAGE_SIZES } from "@/lib/reports/types";

export interface ReportColumn<TRow> {
  key: string;
  label: string;
  align?: "left" | "right" | "center";
  render: (row: TRow) => React.ReactNode;
}

/** first, last, current±1, with "…" gaps — the standard numbered-pagination window. */
function pageWindow(current: number, total: number): (number | "…")[] {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const pages = new Set([1, total, current, current - 1, current + 1]);
  const sorted = [...pages].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  const result: (number | "…")[] = [];
  for (let i = 0; i < sorted.length; i++) {
    if (i > 0 && sorted[i] - sorted[i - 1] > 1) result.push("…");
    result.push(sorted[i]);
  }
  return result;
}

/** The shared table + pagination shell every report / meter-data screen uses — max 100 rows/page,
 * server-side pagination, explicit empty/loading/error states, numbered page controls. */
export function ReportTable<TRow>({
  columns,
  rows,
  rowKey,
  pagination,
  onPageChange,
  onPageSizeChange,
  loading,
  error,
}: {
  columns: ReportColumn<TRow>[];
  rows: TRow[];
  rowKey: (row: TRow) => string;
  pagination: PaginationInfo | undefined;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  loading: boolean;
  error: boolean;
}) {
  const from = pagination && pagination.totalRecords > 0 ? (pagination.page - 1) * pagination.pageSize + 1 : 0;
  const to = pagination ? Math.min(pagination.page * pagination.pageSize, pagination.totalRecords) : 0;

  return (
    <Paper variant="outlined" sx={{ overflowX: "auto" }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            {columns.map((c) => (
              <TableCell key={c.key} align={c.align ?? "left"}>
                {c.label}
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {error && (
            <TableRow>
              <TableCell colSpan={columns.length}>
                <Typography variant="body2" color="error">
                  Unable to load this report. Please retry.
                </Typography>
              </TableCell>
            </TableRow>
          )}
          {!error && loading && (
            <TableRow>
              <TableCell colSpan={columns.length}>
                <Typography variant="body2" color="text.secondary">
                  Loading report…
                </Typography>
              </TableCell>
            </TableRow>
          )}
          {!error && !loading && rows.length === 0 && (
            <TableRow>
              <TableCell colSpan={columns.length}>
                <Typography variant="body2" color="text.secondary">
                  No records found. Try changing your filters or date range.
                </Typography>
              </TableCell>
            </TableRow>
          )}
          {!error &&
            !loading &&
            rows.map((row) => (
              <TableRow key={rowKey(row)} hover>
                {columns.map((c) => (
                  <TableCell key={c.key} align={c.align ?? "left"}>
                    {c.render(row)}
                  </TableCell>
                ))}
              </TableRow>
            ))}
        </TableBody>
      </Table>

      {pagination && pagination.totalRecords > 0 && (
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            px: 2,
            py: 1.5,
            borderTop: "1px solid var(--color-border-default)",
            flexWrap: "wrap",
            gap: 1.5,
          }}
        >
          <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
            <Typography variant="caption" color="text.secondary">
              Show
            </Typography>
            <Select
              size="small"
              value={pagination.pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              sx={{ fontSize: 13 }}
            >
              {REPORT_PAGE_SIZES.map((size) => (
                <MenuItem key={size} value={size} sx={{ fontSize: 13 }}>
                  {size}
                </MenuItem>
              ))}
            </Select>
            <Typography variant="caption" color="text.secondary">
              entries
            </Typography>
          </Box>

          <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
            <IconButton size="small" disabled={pagination.page <= 1} onClick={() => onPageChange(pagination.page - 1)}>
              <ChevronLeftIcon fontSize="small" />
            </IconButton>
            {pageWindow(pagination.page, pagination.totalPages).map((p, i) =>
              p === "…" ? (
                <Typography key={`gap-${i}`} variant="caption" sx={{ px: 0.5, color: "var(--card-muted)" }}>
                  …
                </Typography>
              ) : (
                <Button
                  key={p}
                  size="small"
                  variant={p === pagination.page ? "contained" : "text"}
                  onClick={() => onPageChange(p)}
                  sx={{ minWidth: 30, px: 0, fontSize: 12 }}
                >
                  {p}
                </Button>
              )
            )}
            <IconButton size="small" disabled={pagination.page >= pagination.totalPages} onClick={() => onPageChange(pagination.page + 1)}>
              <ChevronRightIcon fontSize="small" />
            </IconButton>
          </Box>

          <Typography variant="caption" color="text.secondary">
            Showing {from} to {to} of {pagination.totalRecords.toLocaleString()} records
          </Typography>
        </Box>
      )}
    </Paper>
  );
}
