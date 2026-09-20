"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Snackbar } from "@mui/material";
import CloudDownloadOutlinedIcon from "@mui/icons-material/CloudDownloadOutlined";
import NextLink from "next/link";
import { apiClient, ApiError } from "@/lib/apiClient";

/**
 * Files an asynchronous Download Request for the list currently on screen (same filters, every
 * row) instead of streaming it inside this page — for large exports. The file is collected later
 * from the Download Requests screen.
 */
export function RequestDownloadButton({ title, path }: { title: string; path: string }) {
  const queryClient = useQueryClient();
  const [notice, setNotice] = useState<{ severity: "success" | "error"; text: string } | null>(null);

  const request = useMutation({
    mutationFn: () => apiClient.post("/api/v1/download-requests", { title, path }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["download-requests"] });
      setNotice({ severity: "success", text: "Download requested." });
    },
    onError: (err: unknown) => setNotice({ severity: "error", text: err instanceof ApiError ? err.body || err.message : "Request failed." }),
  });

  return (
    <>
      <Button
        variant="outlined"
        size="small"
        startIcon={<CloudDownloadOutlinedIcon fontSize="small" />}
        disabled={request.isPending}
        onClick={() => request.mutate()}
      >
        Request Download
      </Button>
      <Snackbar open={!!notice} autoHideDuration={6000} onClose={() => setNotice(null)} anchorOrigin={{ vertical: "bottom", horizontal: "center" }}>
        {notice ? (
          <Alert
            severity={notice.severity}
            onClose={() => setNotice(null)}
            action={
              notice.severity === "success" ? (
                <Button component={NextLink} href="/downloads" size="small" color="inherit">
                  View
                </Button>
              ) : undefined
            }
          >
            {notice.text}
          </Alert>
        ) : undefined}
      </Snackbar>
    </>
  );
}
