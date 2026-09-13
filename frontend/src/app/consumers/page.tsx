"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Paper,
  Table,
  TableHead,
  TableRow,
  TableCell,
  TableBody,
  TextField,
  Button,
  Stack,
  Alert,
  Chip,
} from "@mui/material";
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import { apiClient, ApiError } from "@/lib/apiClient";
import { PageHeader } from "@/components/PageHeader";

interface ServicePointSummary {
  id: string;
  address: string;
  distributionTransformerNodeId: string | null;
}
interface CustomerResponse {
  id: string;
  accountNumber: string;
  name: string;
  servicePoints: ServicePointSummary[];
}

export default function ConsumersPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [accountNumber, setAccountNumber] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const customersQuery = useQuery({
    queryKey: ["customers", search],
    queryFn: () => apiClient.get<CustomerResponse[]>(`/api/v1/customers${search ? `?search=${encodeURIComponent(search)}` : ""}`),
  });

  const createCustomer = useMutation({
    mutationFn: () => apiClient.post<CustomerResponse>("/api/v1/customers", { accountNumber, name }),
    onSuccess: () => {
      setAccountNumber("");
      setName("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Failed to create consumer."),
  });

  return (
    <Box>
      <PageHeader icon={<GroupsOutlinedIcon fontSize="small" />} title="Consumers" />
      <Stack direction="row" sx={{ mb: 3 }}>
        <Alert severity="info" sx={{ flex: 1 }}>
          Master data only — MDMS records the consumer/service-point identity; tariff, billing, and connection-status facts belong to downstream billing systems, per this data model&rsquo;s own design.
        </Alert>
      </Stack>

      <Paper variant="outlined" sx={{ p: 3, mb: 4 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" } }}>
          <TextField label="Account Number" size="small" value={accountNumber} onChange={(e) => setAccountNumber(e.target.value)} />
          <TextField label="Name" size="small" value={name} onChange={(e) => setName(e.target.value)} sx={{ minWidth: 240 }} />
          <Button variant="contained" disabled={!accountNumber || !name} onClick={() => createCustomer.mutate()}>
            Add Consumer
          </Button>
        </Stack>
      </Paper>

      <TextField
        size="small"
        label="Search"
        placeholder="Account number or name"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        sx={{ mb: 2, minWidth: 280 }}
      />

      <Paper variant="outlined" sx={{ overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Account Number</TableCell>
              <TableCell>Name</TableCell>
              <TableCell>Service Points</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {customersQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={3}>No consumers recorded yet.</TableCell>
              </TableRow>
            )}
            {customersQuery.data?.map((c) => (
              <TableRow key={c.id} hover>
                <TableCell sx={{ fontFamily: "var(--font-mono)", fontSize: 12 }}>{c.accountNumber}</TableCell>
                <TableCell>{c.name}</TableCell>
                <TableCell>
                  {c.servicePoints.length === 0 ? (
                    "—"
                  ) : (
                    <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                      {c.servicePoints.map((sp) => (
                        <Chip key={sp.id} size="small" label={sp.address} variant="outlined" />
                      ))}
                    </Box>
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
