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

/** The shared table + pagination shell every report screen uses — max 100 rows/page, server-side
 * pagination, explicit empty/loading/error states, per the reporting spec. */
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
            gap: 1,
          }}
        >
          <Typography variant="caption" color="text.secondary">
            Showing {from}-{to} of {pagination.totalRecords.toLocaleString()}
          </Typography>
          <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
            <Select
              size="small"
              value={pagination.pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              sx={{ fontSize: 13 }}
            >
              {REPORT_PAGE_SIZES.map((size) => (
                <MenuItem key={size} value={size} sx={{ fontSize: 13 }}>
                  {size} rows
                </MenuItem>
              ))}
            </Select>
            <IconButton size="small" disabled={pagination.page <= 1} onClick={() => onPageChange(pagination.page - 1)}>
              <ChevronLeftIcon fontSize="small" />
            </IconButton>
            <Typography variant="caption">
              Page {pagination.page} of {pagination.totalPages}
            </Typography>
            <IconButton size="small" disabled={pagination.page >= pagination.totalPages} onClick={() => onPageChange(pagination.page + 1)}>
              <ChevronRightIcon fontSize="small" />
            </IconButton>
          </Box>
        </Box>
      )}
    </Paper>
  );
}
