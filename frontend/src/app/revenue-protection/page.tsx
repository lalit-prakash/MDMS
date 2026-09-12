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
} from "@mui/material";
import { apiClient, ApiError } from "@/lib/apiClient";
import { RevenueProtectionLead, RiskSignalType } from "@/lib/revenueProtection";
import { QualityChip } from "@/components/QualityChip";

const SIGNAL_TYPES: RiskSignalType[] = [
  "TamperEvent",
  "RepeatedCoverOpen",
  "ConsumptionDeviation",
  "DtrAnomaly",
  "CommunicationManipulation",
  "UnbilledMappingAnomaly",
];

export default function RevenueProtectionPage() {
  const queryClient = useQueryClient();
  const [customerId, setCustomerId] = useState("");
  const [signalType, setSignalType] = useState<RiskSignalType>("TamperEvent");
  const [weight, setWeight] = useState("20");
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const leadsQuery = useQuery({
    queryKey: ["revenue-protection-leads"],
    queryFn: () => apiClient.get<RevenueProtectionLead[]>("/api/v1/revenue-protection/leads"),
  });

  const raiseSignal = useMutation({
    mutationFn: () =>
      apiClient.post("/api/v1/revenue-protection/signals", {
        customerId,
        meterId: null,
        signalType,
        weight: Number(weight),
        note: note || null,
      }),
    onSuccess: () => {
      setNote("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["revenue-protection-leads"] });
    },
    onError: (err: unknown) =>
      setError(err instanceof ApiError ? err.body || err.message : "Failed to raise signal."),
  });

  const transition = useMutation({
    mutationFn: ({ id, action, body }: { id: string; action: string; body?: unknown }) =>
      apiClient.post(`/api/v1/revenue-protection/leads/${id}/${action}`, body),
    onSuccess: () => {
      setActionError(null);
      queryClient.invalidateQueries({ queryKey: ["revenue-protection-leads"] });
    },
    onError: (err: unknown) =>
      setActionError(err instanceof ApiError ? err.body || err.message : "Action failed."),
  });

  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ fontWeight: 600 }}>
        Revenue Protection
      </Typography>
      <Alert severity="info" sx={{ mb: 3 }}>
        Leads are investigation signals, never a legal conclusion. Signal weights are supplied by
        you here — this project has no built-in opinion on what a signal type is worth.
      </Alert>

      <Typography variant="h6" sx={{ mb: 1 }}>
        Raise a risk signal
      </Typography>
      <Paper variant="outlined" sx={{ p: 3, mb: 4 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, flexWrap: "wrap" }}>
          <TextField label="Customer ID" size="small" value={customerId} onChange={(e) => setCustomerId(e.target.value)} sx={{ minWidth: 260 }} />
          <TextField select label="Signal type" size="small" value={signalType} onChange={(e) => setSignalType(e.target.value as RiskSignalType)} sx={{ minWidth: 220 }}>
            {SIGNAL_TYPES.map((t) => (
              <MenuItem key={t} value={t}>{t}</MenuItem>
            ))}
          </TextField>
          <TextField label="Weight" type="number" size="small" value={weight} onChange={(e) => setWeight(e.target.value)} sx={{ width: 100 }} />
          <TextField label="Note (optional)" size="small" value={note} onChange={(e) => setNote(e.target.value)} sx={{ minWidth: 260 }} />
          <Button variant="contained" disabled={!customerId} onClick={() => raiseSignal.mutate()}>
            Raise
          </Button>
        </Stack>
      </Paper>

      <Typography variant="h6" sx={{ mb: 1 }}>
        Leads (highest risk first)
      </Typography>
      {actionError && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError(null)}>
          {actionError}
        </Alert>
      )}
      <Paper variant="outlined" sx={{ overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Customer</TableCell>
              <TableCell align="right">Risk score</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {leadsQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>No leads yet.</TableCell>
              </TableRow>
            )}
            {leadsQuery.data?.map((lead) => (
              <TableRow key={lead.id} hover>
                <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{lead.customerId}</TableCell>
                <TableCell align="right">{lead.riskScore}</TableCell>
                <TableCell>
                  <QualityChip value={lead.status} />
                </TableCell>
                <TableCell align="right">
                  <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
                    {lead.status === "Scored" && (
                      <Button size="small" onClick={() => transition.mutate({ id: lead.id, action: "review" })}>
                        Review
                      </Button>
                    )}
                    {lead.status === "Reviewed" && (
                      <Button
                        size="small"
                        onClick={() =>
                          transition.mutate({ id: lead.id, action: "assign", body: { userId: crypto.randomUUID() } })
                        }
                      >
                        Assign (demo)
                      </Button>
                    )}
                    {lead.status === "Assigned" && (
                      <Button size="small" onClick={() => transition.mutate({ id: lead.id, action: "start-investigation" })}>
                        Start investigation
                      </Button>
                    )}
                    {lead.status === "FieldInvestigation" && (
                      <Button
                        size="small"
                        onClick={() =>
                          transition.mutate({
                            id: lead.id,
                            action: "record-finding",
                            body: { note: "Recorded from the MDMS console." },
                          })
                        }
                      >
                        Record finding
                      </Button>
                    )}
                    {lead.status === "FindingRecorded" && (
                      <Button
                        size="small"
                        onClick={() =>
                          transition.mutate({
                            id: lead.id,
                            action: "record-action",
                            body: { note: "Action recorded from the MDMS console." },
                          })
                        }
                      >
                        Record action
                      </Button>
                    )}
                    {lead.status === "ActionTaken" && (
                      <Button
                        size="small"
                        onClick={() =>
                          transition.mutate({ id: lead.id, action: "close", body: { note: "Closed from the MDMS console." } })
                        }
                      >
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
