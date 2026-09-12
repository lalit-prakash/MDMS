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
  Button,
  TextField,
  MenuItem,
  Stack,
  Alert,
} from "@mui/material";
import { apiClient, ApiError } from "@/lib/apiClient";
import { Meter, MeterPhase } from "@/lib/types";
import { QualityChip } from "@/components/QualityChip";

export default function MetersPage() {
  const queryClient = useQueryClient();
  const [serialNumber, setSerialNumber] = useState("");
  const [phase, setPhase] = useState<MeterPhase>("Single");
  const [error, setError] = useState<string | null>(null);

  const metersQuery = useQuery({
    queryKey: ["meters"],
    queryFn: () => apiClient.get<Meter[]>("/api/v1/meters"),
  });

  const createMeter = useMutation({
    mutationFn: () => apiClient.post<Meter>("/api/v1/meters", { serialNumber, phase }),
    onSuccess: () => {
      setSerialNumber("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["meters"] });
    },
    onError: (err: unknown) => {
      setError(err instanceof ApiError ? err.body || err.message : "Failed to create meter.");
    },
  });

  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ fontWeight: 600 }}>
        Meters
      </Typography>

      <Paper variant="outlined" sx={{ p: 3, mb: 3 }}>
        <Typography variant="subtitle1" gutterBottom>
          Register a new meter
        </Typography>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" } }}>
          <TextField
            label="Serial number"
            size="small"
            value={serialNumber}
            onChange={(e) => setSerialNumber(e.target.value)}
          />
          <TextField
            select
            label="Phase"
            size="small"
            value={phase}
            onChange={(e) => setPhase(e.target.value as MeterPhase)}
            sx={{ minWidth: 140 }}
          >
            <MenuItem value="Single">Single</MenuItem>
            <MenuItem value="Three">Three</MenuItem>
          </TextField>
          <Button
            variant="contained"
            disabled={!serialNumber || createMeter.isPending}
            onClick={() => createMeter.mutate()}
          >
            Register
          </Button>
        </Stack>
      </Paper>

      <Paper variant="outlined">
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Serial number</TableCell>
              <TableCell>Phase</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Created</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {metersQuery.isLoading && (
              <TableRow>
                <TableCell colSpan={4}>Loading…</TableCell>
              </TableRow>
            )}
            {metersQuery.isError && (
              <TableRow>
                <TableCell colSpan={4}>
                  <Alert severity="warning">
                    Could not reach the backend API. Is it running at{" "}
                    {process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5004"}?
                  </Alert>
                </TableCell>
              </TableRow>
            )}
            {metersQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>No meters registered yet.</TableCell>
              </TableRow>
            )}
            {metersQuery.data?.map((meter) => (
              <TableRow key={meter.id} hover>
                <TableCell>{meter.serialNumber}</TableCell>
                <TableCell>{meter.phase}</TableCell>
                <TableCell>
                  <QualityChip value={meter.status} />
                </TableCell>
                <TableCell>{new Date(meter.createdAtUtc).toLocaleString()}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}
