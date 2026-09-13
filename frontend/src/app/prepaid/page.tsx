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
  Button,
  Stack,
  Alert,
  Grid,
  Card,
  CardContent,
} from "@mui/material";
import { apiClient, ApiError } from "@/lib/apiClient";
import { PrepaidAccount, WalletTransaction } from "@/lib/prepaid";
import { StatusBadge } from "@/components/StatusBadge";
import { MorphingStat } from "@/components/MorphingStat";

import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import { PageHeader } from "@/components/PageHeader";
export default function PrepaidPage() {
  const queryClient = useQueryClient();
  const [customerId, setCustomerId] = useState("");
  const [rechargeAmount, setRechargeAmount] = useState("500");
  const [rechargeReference, setRechargeReference] = useState("");
  const [billingDate, setBillingDate] = useState("");
  const [ratePerKwh, setRatePerKwh] = useState("8");
  const [error, setError] = useState<string | null>(null);

  const accountQuery = useQuery({
    queryKey: ["prepaid-account", customerId],
    queryFn: () => apiClient.get<PrepaidAccount>(`/api/v1/prepaid/accounts/${customerId}`),
    enabled: customerId.length > 0,
    retry: false,
  });

  const transactionsQuery = useQuery({
    queryKey: ["prepaid-transactions", customerId],
    queryFn: () => apiClient.get<WalletTransaction[]>(`/api/v1/prepaid/accounts/${customerId}/transactions`),
    enabled: customerId.length > 0,
    retry: false,
  });

  const invalidateAll = () => {
    queryClient.invalidateQueries({ queryKey: ["prepaid-account", customerId] });
    queryClient.invalidateQueries({ queryKey: ["prepaid-transactions", customerId] });
  };

  const recharge = useMutation({
    mutationFn: () =>
      apiClient.post("/api/v1/prepaid/recharge", {
        customerId,
        amount: Number(rechargeAmount),
        reference: rechargeReference || `PAY-${Date.now()}`,
      }),
    onSuccess: () => {
      setError(null);
      setRechargeReference("");
      invalidateAll();
    },
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Recharge failed."),
  });

  const runBilling = useMutation({
    mutationFn: () =>
      apiClient.post("/api/v1/prepaid/daily-billing", {
        customerId,
        date: billingDate,
        ratePerKwh: Number(ratePerKwh),
      }),
    onSuccess: () => {
      setError(null);
      invalidateAll();
    },
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Billing run failed."),
  });

  const disconnect = useMutation({
    mutationFn: () => apiClient.post(`/api/v1/prepaid/accounts/${customerId}/disconnect`),
    onSuccess: invalidateAll,
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Disconnect failed."),
  });

  const reconnect = useMutation({
    mutationFn: () => apiClient.post(`/api/v1/prepaid/accounts/${customerId}/reconnect`),
    onSuccess: invalidateAll,
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Reconnect failed."),
  });

  return (
    <Box>
      <PageHeader icon={<AccountBalanceWalletOutlinedIcon fontSize="small" />} title="Prepaid" />
      <Alert severity="info" sx={{ mb: 3 }}>
        No HES/meter command integration exists — reconnect only flips this account&rsquo;s own status,
        it never confirms a physical meter action. Daily billing uses a flat caller-supplied rate,
        not real tariff slabs.
      </Alert>

      <TextField
        label="Customer ID"
        size="small"
        value={customerId}
        onChange={(e) => setCustomerId(e.target.value)}
        sx={{ mb: 3, width: 380 }}
      />

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {customerId && accountQuery.data && (
        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid size={{ xs: 6, md: 3 }}>
            <MorphingStat transitionName="kpi-prepaid-balance" label="Balance" value={`₹${accountQuery.data.balance}`} />
          </Grid>
          <Grid size={{ xs: 6, md: 3 }}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="caption" color="text.secondary">
                  Connection
                </Typography>
                <Box sx={{ mt: 0.5 }}>
                  <StatusBadge value={accountQuery.data.isConnected ? "Connected" : "Disconnected"} />
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper variant="outlined" sx={{ p: 3, height: "100%" }}>
            <Typography variant="subtitle1" gutterBottom>
              Recharge
            </Typography>
            <Stack spacing={2}>
              <TextField label="Amount" type="number" size="small" value={rechargeAmount} onChange={(e) => setRechargeAmount(e.target.value)} />
              <TextField
                label="Reference (idempotency key, optional)"
                size="small"
                value={rechargeReference}
                onChange={(e) => setRechargeReference(e.target.value)}
              />
              <Button variant="contained" disabled={!customerId} onClick={() => recharge.mutate()}>
                Recharge
              </Button>
            </Stack>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper variant="outlined" sx={{ p: 3, height: "100%" }}>
            <Typography variant="subtitle1" gutterBottom>
              Run daily billing
            </Typography>
            <Stack spacing={2}>
              <TextField label="Date" type="date" size="small" value={billingDate} onChange={(e) => setBillingDate(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
              <TextField label="Rate per kWh" type="number" size="small" value={ratePerKwh} onChange={(e) => setRatePerKwh(e.target.value)} />
              <Button variant="contained" disabled={!customerId || !billingDate} onClick={() => runBilling.mutate()}>
                Run
              </Button>
            </Stack>
          </Paper>
        </Grid>
      </Grid>

      <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
        <Button variant="outlined" color="error" disabled={!customerId} onClick={() => disconnect.mutate()}>
          Disconnect
        </Button>
        <Button variant="outlined" color="success" disabled={!customerId} onClick={() => reconnect.mutate()}>
          Reconnect
        </Button>
      </Stack>

      <Typography variant="h6" sx={{ mb: 1 }}>
        Ledger (most recent 500)
      </Typography>
      <Paper variant="outlined" sx={{ overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Type</TableCell>
              <TableCell align="right">Amount</TableCell>
              <TableCell align="right">Balance after</TableCell>
              <TableCell>Reference</TableCell>
              <TableCell>Time</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(!customerId || transactionsQuery.data?.length === 0) && (
              <TableRow>
                <TableCell colSpan={5}>{customerId ? "No transactions yet." : "Enter a customer ID above."}</TableCell>
              </TableRow>
            )}
            {transactionsQuery.data?.map((t) => (
              <TableRow key={t.id} hover>
                <TableCell>{t.type}</TableCell>
                <TableCell align="right">{t.amount}</TableCell>
                <TableCell align="right">{t.balanceAfter}</TableCell>
                <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{t.reference}</TableCell>
                <TableCell>{new Date(t.createdAtUtc).toLocaleString()}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}
