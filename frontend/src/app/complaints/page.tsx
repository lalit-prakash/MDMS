"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Typography,
  Paper,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  TextField,
  MenuItem,
  Button,
  Stack,
  Alert,
  FormControlLabel,
  Switch,
} from "@mui/material";
import { apiClient, ApiError } from "@/lib/apiClient";
import { Complaint, ComplaintSource } from "@/lib/complaints";
import { StatusBadge } from "@/components/StatusBadge";

import SupportAgentOutlinedIcon from "@mui/icons-material/SupportAgentOutlined";
import { PageHeader } from "@/components/PageHeader";
const SOURCES: ComplaintSource[] = ["ConsumerPortal", "MobileApp", "Helpline1912"];

export default function ComplaintsPage() {
  const queryClient = useQueryClient();
  const [customerId, setCustomerId] = useState("");
  const [description, setDescription] = useState("");
  const [source, setSource] = useState<ComplaintSource>("ConsumerPortal");
  const [slaHours, setSlaHours] = useState("48");
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const complaintsQuery = useQuery({
    queryKey: ["complaints", overdueOnly],
    queryFn: () =>
      apiClient.get<Complaint[]>(`/api/v1/complaints${overdueOnly ? "?overdueOnly=true" : ""}`),
  });

  const create = useMutation({
    mutationFn: () =>
      apiClient.post<Complaint>("/api/v1/complaints", {
        customerId,
        meterId: null,
        source,
        description,
        slaHours: Number(slaHours),
      }),
    onSuccess: () => {
      setDescription("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["complaints"] });
    },
    onError: (err: unknown) =>
      setError(err instanceof ApiError ? err.body || err.message : "Failed to create complaint."),
  });

  const transition = useMutation({
    mutationFn: ({ id, action, body }: { id: string; action: string; body?: unknown }) =>
      apiClient.post(`/api/v1/complaints/${id}/${action}`, body),
    onSuccess: () => {
      setActionError(null);
      queryClient.invalidateQueries({ queryKey: ["complaints"] });
    },
    onError: (err: unknown) =>
      setActionError(err instanceof ApiError ? err.body || err.message : "Action failed."),
  });

  return (
    <Box>
      <PageHeader icon={<SupportAgentOutlinedIcon fontSize="small" />} title="Complaints" />

      <Paper variant="outlined" sx={{ p: 3, mb: 3 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, flexWrap: "wrap" }}>
          <TextField label="Customer ID" size="small" value={customerId} onChange={(e) => setCustomerId(e.target.value)} sx={{ minWidth: 260 }} />
          <TextField select label="Source" size="small" value={source} onChange={(e) => setSource(e.target.value as ComplaintSource)} sx={{ minWidth: 160 }}>
            {SOURCES.map((s) => (
              <MenuItem key={s} value={s}>{s}</MenuItem>
            ))}
          </TextField>
          <TextField label="Description" size="small" value={description} onChange={(e) => setDescription(e.target.value)} sx={{ minWidth: 300 }} />
          <TextField label="SLA (hours)" type="number" size="small" value={slaHours} onChange={(e) => setSlaHours(e.target.value)} sx={{ width: 120 }} />
          <Button variant="contained" disabled={!customerId || !description} onClick={() => create.mutate()}>
            Raise
          </Button>
        </Stack>
      </Paper>

      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", mb: 1 }}>
        <Typography variant="h6">Tickets</Typography>
        <FormControlLabel
          control={<Switch checked={overdueOnly} onChange={(e) => setOverdueOnly(e.target.checked)} />}
          label="Overdue only"
        />
      </Stack>
      {actionError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError(null)}>
          {actionError}
        </Alert>
      )}
      <Paper variant="outlined" sx={{ overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Description</TableCell>
              <TableCell>Source</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>SLA due</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {complaintsQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>No complaints found.</TableCell>
              </TableRow>
            )}
            {complaintsQuery.data?.map((c) => (
              <TableRow key={c.id} hover>
                <TableCell>{c.description}</TableCell>
                <TableCell>{c.source}</TableCell>
                <TableCell>
                  <StatusBadge value={c.status} />
                </TableCell>
                <TableCell>{new Date(c.slaDueUtc).toLocaleString()}</TableCell>
                <TableCell align="right">
                  <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
                    {c.status === "Open" && (
                      <Button
                        size="small"
                        onClick={() =>
                          transition.mutate({ id: c.id, action: "assign", body: { userId: crypto.randomUUID() } })
                        }
                      >
                        Assign (demo)
                      </Button>
                    )}
                    {c.status === "Assigned" && (
                      <Button size="small" onClick={() => transition.mutate({ id: c.id, action: "start-progress" })}>
                        Start
                      </Button>
                    )}
                    {c.status === "InProgress" && (
                      <Button
                        size="small"
                        onClick={() =>
                          transition.mutate({
                            id: c.id,
                            action: "resolve",
                            body: { resolutionNote: "Resolved from the MDMS console." },
                          })
                        }
                      >
                        Resolve
                      </Button>
                    )}
                    {c.status === "Resolved" && (
                      <Button size="small" onClick={() => transition.mutate({ id: c.id, action: "close" })}>
                        Close
                      </Button>
                    )}
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}
