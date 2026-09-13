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
import { MeasurementRangeThreshold, MeasurementRangeType, VeeExecutionRecord } from "@/lib/types";
import { StatusBadge } from "@/components/StatusBadge";

import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import { PageHeader } from "@/components/PageHeader";
export default function VeePage() {
  const queryClient = useQueryClient();
  const [measurementType, setMeasurementType] = useState<MeasurementRangeType>("LoadSurveyInterval");
  const [meterId, setMeterId] = useState("");
  const [min, setMin] = useState("0");
  const [max, setMax] = useState("100");
  const [error, setError] = useState<string | null>(null);
  const [runMeterId, setRunMeterId] = useState("");
  const [runDate, setRunDate] = useState("");

  const thresholdsQuery = useQuery({
    queryKey: ["vee-thresholds"],
    queryFn: () => apiClient.get<MeasurementRangeThreshold[]>("/api/v1/vee/thresholds"),
  });

  const recordsQuery = useQuery({
    queryKey: ["vee-execution-records"],
    queryFn: () => apiClient.get<VeeExecutionRecord[]>("/api/v1/vee/execution-records"),
  });

  const createThreshold = useMutation({
    mutationFn: () =>
      apiClient.post<MeasurementRangeThreshold>("/api/v1/vee/thresholds", {
        measurementType,
        meterId: meterId || null,
        minConsumptionKwh: Number(min),
        maxConsumptionKwh: Number(max),
      }),
    onSuccess: () => {
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["vee-thresholds"] });
    },
    onError: (err: unknown) =>
      setError(err instanceof ApiError ? err.body || err.message : "Failed to create threshold."),
  });

  const runEstimation = useMutation({
    mutationFn: () =>
      apiClient.post(`/api/v1/vee/estimation/ls/run?meterId=${runMeterId}&date=${runDate}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["vee-execution-records"] }),
  });

  return (
    <Box>
      <PageHeader icon={<FactCheckOutlinedIcon fontSize="small" />} title="VEE" />

      <Typography variant="h6" sx={{ mb: 1 }}>
        Out-of-range thresholds
      </Typography>
      <Paper variant="outlined" sx={{ p: 3, mb: 2 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, flexWrap: "wrap" }}>
          <TextField
            select
            label="Measurement type"
            size="small"
            value={measurementType}
            onChange={(e) => setMeasurementType(e.target.value as MeasurementRangeType)}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="LoadSurveyInterval">Load Survey Interval</MenuItem>
            <MenuItem value="DailyLoadProfile">Daily Load Profile</MenuItem>
          </TextField>
          <TextField
            label="Meter ID (blank = global default)"
            size="small"
            value={meterId}
            onChange={(e) => setMeterId(e.target.value)}
            sx={{ minWidth: 260 }}
          />
          <TextField label="Min kWh" size="small" type="number" value={min} onChange={(e) => setMin(e.target.value)} sx={{ width: 110 }} />
          <TextField label="Max kWh" size="small" type="number" value={max} onChange={(e) => setMax(e.target.value)} sx={{ width: 110 }} />
          <Button variant="contained" onClick={() => createThreshold.mutate()} disabled={createThreshold.isPending}>
            Save threshold
          </Button>
        </Stack>
      </Paper>
      <Paper variant="outlined" sx={{ mb: 4, overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Type</TableCell>
              <TableCell>Meter ID</TableCell>
              <TableCell align="right">Min</TableCell>
              <TableCell align="right">Max</TableCell>
              <TableCell>Active</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {thresholdsQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>No thresholds configured.</TableCell>
              </TableRow>
            )}
            {thresholdsQuery.data?.map((t) => (
              <TableRow key={t.id} hover>
                <TableCell>{t.measurementType}</TableCell>
                <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>
                  {t.meterId ?? "(global default)"}
                </TableCell>
                <TableCell align="right">{t.minConsumptionKwh}</TableCell>
                <TableCell align="right">{t.maxConsumptionKwh}</TableCell>
                <TableCell>{t.isActive ? "Yes" : "No"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Typography variant="h6" sx={{ mb: 1 }}>
        Run missing-interval estimation (Load Survey)
      </Typography>
      <Paper variant="outlined" sx={{ p: 3, mb: 4 }}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" } }}>
          <TextField
            label="Meter ID"
            size="small"
            value={runMeterId}
            onChange={(e) => setRunMeterId(e.target.value)}
            sx={{ minWidth: 260 }}
          />
          <TextField
            label="Date"
            type="date"
            size="small"
            value={runDate}
            onChange={(e) => setRunDate(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <Button
            variant="contained"
            disabled={!runMeterId || !runDate || runEstimation.isPending}
            onClick={() => runEstimation.mutate()}
          >
            Run
          </Button>
        </Stack>
      </Paper>

      <Typography variant="h6" sx={{ mb: 1 }}>
        VEE execution records (audit trail, most recent 500)
      </Typography>
      <Paper variant="outlined" sx={{ overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Rule</TableCell>
              <TableCell>Slot start</TableCell>
              <TableCell>Result</TableCell>
              <TableCell align="right">Value</TableCell>
              <TableCell>Details</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {recordsQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>No execution records yet.</TableCell>
              </TableRow>
            )}
            {recordsQuery.data?.map((r) => (
              <TableRow key={r.id} hover>
                <TableCell>{r.ruleName}</TableCell>
                <TableCell>{new Date(r.slotStartUtc).toLocaleString()}</TableCell>
                <TableCell>
                  <StatusBadge value={r.resultQuality} />
                </TableCell>
                <TableCell align="right">{r.newValue ?? "—"}</TableCell>
                <TableCell>{r.details}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}
