"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Box, Button, IconButton, Paper } from "@mui/material";
import CloudDownloadOutlinedIcon from "@mui/icons-material/CloudDownloadOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import FileDownloadOutlinedIcon from "@mui/icons-material/FileDownloadOutlined";
import { apiClient } from "@/lib/apiClient";
import { downloadFile } from "@/lib/reports/downloadFile";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { ReportTable, ReportColumn } from "@/components/reports/ReportTable";

interface DownloadRequestRow {
  id: string;
  title: string;
  status: "Pending" | "Processing" | "Completed" | "Failed";
  requestedAtUtc: string;
  completedAtUtc: string | null;
  fileName: string | null;
  sizeBytes: number | null;
  rowCount: number | null;
  errorMessage: string | null;
}

function formatSize(bytes: number | null) {
  if (bytes === null) return "—";
  return bytes < 1024 * 1024 ? `${(bytes / 1024).toFixed(1)} KB` : `${(bytes / 1024 / 1024).toFixed(2)} MB`;
}

export default function DownloadRequestsPage() {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: ["download-requests"],
    queryFn: () => apiClient.get<DownloadRequestRow[]>("/api/v1/download-requests"),
    // Poll only while something is still in flight.
    refetchInterval: (q) => (q.state.data?.some((r) => r.status === "Pending" || r.status === "Processing") ? 2000 : false),
  });

  const remove = useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/v1/download-requests/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["download-requests"] }),
  });

  const columns: ReportColumn<DownloadRequestRow>[] = [
    { key: "title", label: "Request", render: (r) => r.title },
    { key: "requested", label: "Requested", render: (r) => new Date(r.requestedAtUtc).toLocaleString() },
    { key: "status", label: "Status", render: (r) => <StatusBadge value={r.status} /> },
    { key: "rows", label: "Rows", align: "right", render: (r) => r.rowCount ?? "—" },
    { key: "size", label: "Size", align: "right", render: (r) => formatSize(r.sizeBytes) },
    { key: "error", label: "Details", render: (r) => r.errorMessage ?? r.fileName ?? "—" },
    {
      key: "action",
      label: "Action",
      align: "right",
      render: (r) => (
        <Box sx={{ display: "flex", justifyContent: "flex-end", gap: 0.5 }}>
          {r.status === "Completed" && (
            <Button
              size="small"
              startIcon={<FileDownloadOutlinedIcon fontSize="small" />}
              onClick={() => downloadFile(`/api/v1/download-requests/${r.id}/file`, r.fileName ?? "MDMS_Export.csv")}
            >
              Download
            </Button>
          )}
          <IconButton size="small" aria-label={`Delete ${r.title}`} onClick={() => remove.mutate(r.id)}>
            <DeleteOutlineOutlinedIcon fontSize="small" />
          </IconButton>
        </Box>
      ),
    },
  ];

  return (
    <Box>
      <PageHeader icon={<CloudDownloadOutlinedIcon fontSize="small" />} title="Download Requests" />
      <Paper variant="outlined" sx={{ p: 0 }}>
        <ReportTable
          columns={columns}
          rows={query.data ?? []}
          rowKey={(r) => r.id}
          pagination={undefined}
          onPageChange={() => {}}
          onPageSizeChange={() => {}}
          loading={query.isLoading}
          error={query.isError}
        />
      </Paper>
    </Box>
  );
}
