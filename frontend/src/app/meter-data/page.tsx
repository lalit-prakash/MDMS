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
  Stack,
  Button,
  Switch,
  FormControlLabel,
  Alert,
} from "@mui/material";
import { apiClient } from "@/lib/apiClient";
import { LoadSurveyInterval, DailyLoadProfile, DataQualityHold } from "@/lib/types";
import { StatusBadge } from "@/components/StatusBadge";

import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import { PageHeader } from "@/components/PageHeader";
export default function MeterDataPage() {
  const queryClient = useQueryClient();
  const [meterId, setMeterId] = useState("");
  const [activeOnly, setActiveOnly] = useState(true);

  const lsQuery = useQuery({
    queryKey: ["ls-intervals", meterId],
    queryFn: () =>
      apiClient.get<LoadSurveyInterval[]>(
        `/api/v1/meter-data/ls${meterId ? `?meterId=${meterId}` : ""}`
      ),
  });

  const dlpQuery = useQuery({
    queryKey: ["dlp", meterId],
    queryFn: () =>
      apiClient.get<DailyLoadProfile[]>(
        `/api/v1/meter-data/dlp${meterId ? `?meterId=${meterId}` : ""}`
      ),
  });

  const holdsQuery = useQuery({
    queryKey: ["billing-holds", activeOnly],
    queryFn: () =>
      apiClient.get<DataQualityHold[]>(`/api/v1/meter-data/billing-holds?activeOnly=${activeOnly}`),
  });

  const clearHold = useMutation({
    mutationFn: (meterIdToClear: string) =>
      apiClient.post(`/api/v1/meter-data/${meterIdToClear}/billing-hold/clear`, {
        resolutionNote: "Cleared from the MDMS console.",
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["billing-holds"] }),
  });

  return (
    <Box>
      <PageHeader icon={<StorageOutlinedIcon fontSize="small" />} title="Meter Data" />

      <TextField
        label="Filter by Meter ID (optional)"
        size="small"
        value={meterId}
        onChange={(e) => setMeterId(e.target.value)}
        sx={{ mb: 3, width: 380 }}
      />

      <Typography variant="h6" sx={{ mb: 1 }}>
        Load Survey intervals (most recent 500)
      </Typography>
      <Paper variant="outlined" sx={{ mb: 4, overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Interval start</TableCell>
              <TableCell>Interval end</TableCell>
              <TableCell align="right">Cumulative</TableCell>
              <TableCell align="right">Consumption (kWh)</TableCell>
              <TableCell>Quality</TableCell>
              <TableCell>Source</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {lsQuery.isError && (
              <TableRow>
                <TableCell colSpan={6}>
                  <Alert severity="warning">Could not load LS intervals.</Alert>
                </TableCell>
              </TableRow>
            )}
            {lsQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={6}>No intervals found.</TableCell>
              </TableRow>
            )}
            {lsQuery.data?.map((i) => (
              <TableRow key={i.id} hover>
                <TableCell>{new Date(i.intervalStartUtc).toLocaleString()}</TableCell>
                <TableCell>{new Date(i.intervalEndUtc).toLocaleString()}</TableCell>
                <TableCell align="right">{i.cumulativeReading}</TableCell>
                <TableCell align="right">{i.consumptionKwh}</TableCell>
                <TableCell>
                  <StatusBadge value={i.quality} />
                </TableCell>
                <TableCell>
                  <StatusBadge value={i.source} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Typography variant="h6" sx={{ mb: 1 }}>
        Daily Load Profiles (most recent 500)
      </Typography>
      <Paper variant="outlined" sx={{ mb: 4, overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Profile date</TableCell>
              <TableCell align="right">Consumption (kWh)</TableCell>
              <TableCell>Quality</TableCell>
              <TableCell>Source</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {dlpQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>No profiles found.</TableCell>
              </TableRow>
            )}
            {dlpQuery.data?.map((p) => (
              <TableRow key={p.id} hover>
                <TableCell>{p.profileDate}</TableCell>
                <TableCell align="right">{p.consumptionKwh}</TableCell>
                <TableCell>
                  <StatusBadge value={p.quality} />
                </TableCell>
                <TableCell>
                  <StatusBadge value={p.source} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", mb: 1 }}>
        <Typography variant="h6">Data-quality holds</Typography>
        <FormControlLabel
          control={<Switch checked={activeOnly} onChange={(e) => setActiveOnly(e.target.checked)} />}
          label="Active only"
        />
      </Stack>
      <Paper variant="outlined" sx={{ overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Meter ID</TableCell>
              <TableCell>Reason</TableCell>
              <TableCell>Raised</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Action</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {holdsQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>No holds found.</TableCell>
              </TableRow>
            )}
            {holdsQuery.data?.map((h) => (
              <TableRow key={h.id} hover>
                <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{h.meterId}</TableCell>
                <TableCell>{h.reason}</TableCell>
                <TableCell>{new Date(h.raisedAtUtc).toLocaleString()}</TableCell>
                <TableCell>
                  <StatusBadge value={h.isActive ? "Active" : "Cleared"} />
                </TableCell>
                <TableCell align="right">
                  {h.isActive && (
                    <Button size="small" onClick={() => clearHold.mutate(h.meterId)}>
                      Clear
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}
