"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Typography,
  Paper,
  TextField,
  Button,
  Stack,
  Alert,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  Grid,
} from "@mui/material";
import { apiClient, ApiError } from "@/lib/apiClient";
import { MorphingStat } from "@/components/MorphingStat";

interface EnergyBalanceResult {
  hierarchyNodeId: string;
  date: string;
  inputEnergyKwh: number | null;
  accountedEnergyKwh: number;
  discrepancyKwh: number | null;
  discrepancyPercent: number | null;
  linkedServicePointCount: number;
  servicePointsWithDataCount: number;
  dataCompletenessPercent: number | null;
}

interface NetworkEnergyReading {
  id: string;
  hierarchyNodeId: string;
  date: string;
  energyKwh: number;
}

export default function EnergyAuditPage() {
  const queryClient = useQueryClient();
  const [nodeId, setNodeId] = useState("");
  const [readingDate, setReadingDate] = useState("");
  const [energyKwh, setEnergyKwh] = useState("");
  const [error, setError] = useState<string | null>(null);

  const [balanceNodeId, setBalanceNodeId] = useState("");
  const [balanceDate, setBalanceDate] = useState("");
  const [balance, setBalance] = useState<EnergyBalanceResult | null>(null);

  const recordReading = useMutation({
    mutationFn: () =>
      apiClient.post("/api/v1/energy-audit/network-energy-readings", {
        hierarchyNodeId: nodeId,
        date: readingDate,
        energyKwh: Number(energyKwh),
      }),
    onSuccess: () => {
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["network-energy-readings"] });
    },
    onError: (err: unknown) =>
      setError(err instanceof ApiError ? err.body || err.message : "Failed to record reading."),
  });

  const readingsQuery = useQuery({
    queryKey: ["network-energy-readings"],
    queryFn: () => apiClient.get<NetworkEnergyReading[]>("/api/v1/energy-audit/network-energy-readings"),
  });

  const balanceMutation = useMutation({
    mutationFn: () =>
      apiClient.get<EnergyBalanceResult>(
        `/api/v1/energy-audit/balance?hierarchyNodeId=${balanceNodeId}&date=${balanceDate}`
      ),
    onSuccess: (data) => setBalance(data),
  });

  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ fontWeight: 600 }}>
        Energy Audit
      </Typography>
      <Alert severity="info" sx={{ mb: 3 }}>
        No feeder/DTR meter ingestion pipeline exists yet — a supply-side reading is entered
        manually below rather than derived from a real boundary meter feed.
      </Alert>

      <Typography variant="h6" sx={{ mb: 1 }}>
        Record a network energy reading
      </Typography>
      <Paper variant="outlined" sx={{ p: 3, mb: 4 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" } }}>
          <TextField label="Hierarchy Node ID" size="small" value={nodeId} onChange={(e) => setNodeId(e.target.value)} sx={{ minWidth: 260 }} />
          <TextField label="Date" type="date" size="small" value={readingDate} onChange={(e) => setReadingDate(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField label="Energy (kWh)" type="number" size="small" value={energyKwh} onChange={(e) => setEnergyKwh(e.target.value)} sx={{ width: 140 }} />
          <Button variant="contained" disabled={!nodeId || !readingDate || !energyKwh} onClick={() => recordReading.mutate()}>
            Save
          </Button>
        </Stack>
      </Paper>

      <Typography variant="h6" sx={{ mb: 1 }}>
        Compute a daily energy balance
      </Typography>
      <Paper variant="outlined" sx={{ p: 3, mb: 3 }}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" } }}>
          <TextField label="Hierarchy Node ID" size="small" value={balanceNodeId} onChange={(e) => setBalanceNodeId(e.target.value)} sx={{ minWidth: 260 }} />
          <TextField label="Date" type="date" size="small" value={balanceDate} onChange={(e) => setBalanceDate(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <Button variant="contained" disabled={!balanceNodeId || !balanceDate} onClick={() => balanceMutation.mutate()}>
            Compute
          </Button>
        </Stack>
      </Paper>

      {balance && (
        <Grid container spacing={2} sx={{ mb: 4 }}>
          <Grid size={{ xs: 6, md: 3 }}>
            <MorphingStat
              transitionName="kpi-input-energy"
              label="Input energy (kWh)"
              value={balance.inputEnergyKwh?.toString() ?? "No reading"}
            />
          </Grid>
          <Grid size={{ xs: 6, md: 3 }}>
            <MorphingStat
              transitionName="kpi-accounted-energy"
              label="Accounted energy (kWh)"
              value={balance.accountedEnergyKwh.toString()}
            />
          </Grid>
          <Grid size={{ xs: 6, md: 3 }}>
            <MorphingStat
              transitionName="kpi-discrepancy"
              label="Discrepancy"
              value={
                balance.discrepancyKwh === null
                  ? "N/A"
                  : `${balance.discrepancyKwh} kWh (${balance.discrepancyPercent?.toFixed(1)}%)`
              }
            />
          </Grid>
          <Grid size={{ xs: 6, md: 3 }}>
            <MorphingStat
              transitionName="kpi-data-completeness"
              label="Data completeness"
              value={
                balance.dataCompletenessPercent === null
                  ? "N/A"
                  : `${balance.dataCompletenessPercent.toFixed(1)}% (${balance.servicePointsWithDataCount}/${balance.linkedServicePointCount})`
              }
            />
          </Grid>
        </Grid>
      )}

      <Typography variant="h6" sx={{ mb: 1 }}>
        Recorded network energy readings (most recent 500)
      </Typography>
      <Paper variant="outlined" sx={{ overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Hierarchy Node</TableCell>
              <TableCell>Date</TableCell>
              <TableCell align="right">Energy (kWh)</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {readingsQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={3}>No readings recorded yet.</TableCell>
              </TableRow>
            )}
            {readingsQuery.data?.map((r) => (
              <TableRow key={r.id} hover>
                <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{r.hierarchyNodeId}</TableCell>
                <TableCell>{r.date}</TableCell>
                <TableCell align="right">{r.energyKwh}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}
