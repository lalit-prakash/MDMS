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
  Tabs,
  Tab,
} from "@mui/material";
import { apiClient } from "@/lib/apiClient";
import { LoadSurveyInterval, DailyLoadProfile, DataQualityHold } from "@/lib/types";
import { StatusBadge } from "@/components/StatusBadge";

import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import { PageHeader } from "@/components/PageHeader";

interface InstantaneousProfile {
  id: string;
  meterId: string;
  meterTimeUtc: string;
  voltage: number;
  phaseCurrent: number;
  powerFactor: number;
  frequency: number;
  kw: number;
  kva: number;
  kwh: number;
  kvah: number;
  loadLimitState: string;
  tamperCount: number;
}

interface BillingProfile {
  id: string;
  meterId: string;
  billingDate: string;
  cumulativeKwhImport: number;
  cumulativeKvahImport: number;
  averagePowerFactor: number;
  maximumDemandKw: number;
  maximumDemandKva: number;
}

interface MeterEvent {
  id: string;
  meterId: string;
  occurredAtUtc: string;
  eventType: string;
  severity: string;
  description: string | null;
  isAcknowledged: boolean;
}

type DataTab = "ls" | "dlp" | "ip" | "bp" | "events" | "holds";

const TABS: { value: DataTab; label: string }[] = [
  { value: "ls", label: "Load Survey (LS)" },
  { value: "dlp", label: "Daily Profile (DP)" },
  { value: "ip", label: "Instantaneous Profile (IP)" },
  { value: "bp", label: "Billing Profile (BP)" },
  { value: "events", label: "Events & Alarms" },
  { value: "holds", label: "Data-Quality Holds" },
];

export default function MeterDataPage() {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<DataTab>("ls");
  const [meterId, setMeterId] = useState("");
  const [activeOnly, setActiveOnly] = useState(true);

  const lsQuery = useQuery({
    queryKey: ["ls-intervals", meterId],
    queryFn: () => apiClient.get<LoadSurveyInterval[]>(`/api/v1/meter-data/ls${meterId ? `?meterId=${meterId}` : ""}`),
    enabled: tab === "ls",
  });

  const dlpQuery = useQuery({
    queryKey: ["dlp", meterId],
    queryFn: () => apiClient.get<DailyLoadProfile[]>(`/api/v1/meter-data/dlp${meterId ? `?meterId=${meterId}` : ""}`),
    enabled: tab === "dlp",
  });

  const ipQuery = useQuery({
    queryKey: ["ip", meterId],
    queryFn: () => apiClient.get<InstantaneousProfile[]>(`/api/v1/meter-data/ip${meterId ? `?meterId=${meterId}` : ""}`),
    enabled: tab === "ip",
  });

  const bpQuery = useQuery({
    queryKey: ["bp", meterId],
    queryFn: () => apiClient.get<BillingProfile[]>(`/api/v1/meter-data/bp${meterId ? `?meterId=${meterId}` : ""}`),
    enabled: tab === "bp",
  });

  const eventsQuery = useQuery({
    queryKey: ["meter-events", meterId],
    queryFn: () => apiClient.get<MeterEvent[]>(`/api/v1/meter-data/events${meterId ? `?meterId=${meterId}` : ""}`),
    enabled: tab === "events",
  });

  const acknowledgeEvent = useMutation({
    mutationFn: (eventId: string) => apiClient.post(`/api/v1/meter-data/events/${eventId}/acknowledge`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["meter-events"] }),
  });

  const holdsQuery = useQuery({
    queryKey: ["billing-holds", activeOnly],
    queryFn: () => apiClient.get<DataQualityHold[]>(`/api/v1/meter-data/billing-holds?activeOnly=${activeOnly}`),
    enabled: tab === "holds",
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
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Every category of meter-reported data this system records: 30-minute Load Survey, Daily
        Profile, 15-minute Instantaneous Profile, monthly Billing Profile, and Events/Alarms.
      </Typography>

      <Tabs value={tab} onChange={(_e, v) => setTab(v)} sx={{ mb: 3, borderBottom: "1px solid var(--color-border-default)" }}>
        {TABS.map((t) => (
          <Tab key={t.value} value={t.value} label={t.label} sx={{ textTransform: "none", fontSize: 13 }} />
        ))}
      </Tabs>

      {tab !== "holds" && (
        <TextField
          label="Filter by Meter ID (optional)"
          size="small"
          value={meterId}
          onChange={(e) => setMeterId(e.target.value)}
          sx={{ mb: 3, width: 380 }}
        />
      )}

      {tab === "ls" && (
        <Paper variant="outlined" sx={{ overflowX: "auto" }}>
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
                  <TableCell><StatusBadge value={i.quality} /></TableCell>
                  <TableCell><StatusBadge value={i.source} /></TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      {tab === "dlp" && (
        <Paper variant="outlined" sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Profile date</TableCell>
                <TableCell align="right">kWh Import</TableCell>
                <TableCell align="right">kVAh Import</TableCell>
                <TableCell align="right">kWh Export</TableCell>
                <TableCell align="right">kVAh Export</TableCell>
                <TableCell>Quality</TableCell>
                <TableCell>Source</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {dlpQuery.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>No profiles found.</TableCell>
                </TableRow>
              )}
              {dlpQuery.data?.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{p.profileDate}</TableCell>
                  <TableCell align="right">{p.consumptionKwh}</TableCell>
                  <TableCell align="right">{p.kvahImport ?? "—"}</TableCell>
                  <TableCell align="right">{p.kwhExport ?? "—"}</TableCell>
                  <TableCell align="right">{p.kvahExport ?? "—"}</TableCell>
                  <TableCell><StatusBadge value={p.quality} /></TableCell>
                  <TableCell><StatusBadge value={p.source} /></TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      {tab === "ip" && (
        <Paper variant="outlined" sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Meter Time</TableCell>
                <TableCell align="right">Voltage</TableCell>
                <TableCell align="right">Current</TableCell>
                <TableCell align="right">PF</TableCell>
                <TableCell align="right">Freq</TableCell>
                <TableCell align="right">kW</TableCell>
                <TableCell align="right">kVA</TableCell>
                <TableCell align="right">kWh</TableCell>
                <TableCell align="right">kVAh</TableCell>
                <TableCell>Load Limit</TableCell>
                <TableCell align="right">Tamper</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {ipQuery.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={11}>No Instantaneous Profile readings found.</TableCell>
                </TableRow>
              )}
              {ipQuery.data?.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{new Date(p.meterTimeUtc).toLocaleString()}</TableCell>
                  <TableCell align="right">{p.voltage}</TableCell>
                  <TableCell align="right">{p.phaseCurrent}</TableCell>
                  <TableCell align="right">{p.powerFactor}</TableCell>
                  <TableCell align="right">{p.frequency}</TableCell>
                  <TableCell align="right">{p.kw}</TableCell>
                  <TableCell align="right">{p.kva}</TableCell>
                  <TableCell align="right">{p.kwh}</TableCell>
                  <TableCell align="right">{p.kvah}</TableCell>
                  <TableCell><StatusBadge value={p.loadLimitState} /></TableCell>
                  <TableCell align="right">{p.tamperCount}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      {tab === "bp" && (
        <Paper variant="outlined" sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Billing Date</TableCell>
                <TableCell align="right">Cumulative kWh Import</TableCell>
                <TableCell align="right">Cumulative kVAh Import</TableCell>
                <TableCell align="right">Avg. PF</TableCell>
                <TableCell align="right">MD kW</TableCell>
                <TableCell align="right">MD kVA</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {bpQuery.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>No Billing Profiles found.</TableCell>
                </TableRow>
              )}
              {bpQuery.data?.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{p.billingDate}</TableCell>
                  <TableCell align="right">{p.cumulativeKwhImport}</TableCell>
                  <TableCell align="right">{p.cumulativeKvahImport}</TableCell>
                  <TableCell align="right">{p.averagePowerFactor}</TableCell>
                  <TableCell align="right">{p.maximumDemandKw}</TableCell>
                  <TableCell align="right">{p.maximumDemandKva}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      {tab === "events" && (
        <Paper variant="outlined" sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Occurred</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Severity</TableCell>
                <TableCell>Description</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {eventsQuery.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6}>No events or alarms found.</TableCell>
                </TableRow>
              )}
              {eventsQuery.data?.map((e) => (
                <TableRow key={e.id} hover>
                  <TableCell>{new Date(e.occurredAtUtc).toLocaleString()}</TableCell>
                  <TableCell>{e.eventType}</TableCell>
                  <TableCell><StatusBadge value={e.severity} /></TableCell>
                  <TableCell>{e.description ?? "—"}</TableCell>
                  <TableCell><StatusBadge value={e.isAcknowledged ? "Acknowledged" : "Open"} /></TableCell>
                  <TableCell align="right">
                    {!e.isAcknowledged && (
                      <Button size="small" onClick={() => acknowledgeEvent.mutate(e.id)}>
                        Acknowledge
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}

      {tab === "holds" && (
        <>
          <Stack direction="row" sx={{ alignItems: "center", justifyContent: "flex-end", mb: 1 }}>
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
                    <TableCell><StatusBadge value={h.isActive ? "Active" : "Cleared"} /></TableCell>
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
        </>
      )}
    </Box>
  );
}
