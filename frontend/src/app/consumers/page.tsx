"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Box, Paper, TextField, Button, Stack, Alert } from "@mui/material";
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import { apiClient, ApiError } from "@/lib/apiClient";
import { PageHeader } from "@/components/PageHeader";
import { ReportColumn } from "@/components/reports/ReportTable";
import { MasterDataListView } from "@/components/reports/MasterDataListView";

interface ConsumerMasterRow {
  id: string;
  accountNumber: string;
  name: string;
  rrNumber: string | null;
  meterNumber: string | null;
  dtrCode: string | null;
  feederCode: string | null;
  substationCode: string | null;
  zone: string | null;
  circle: string | null;
  division: string | null;
  subDivision: string | null;
  section: string | null;
  address: string;
  mobileNumber: string | null;
  connectionStatus: string | null;
  serviceDate: string | null;
  sanctionedLoadKw: number | null;
  contractDemandKva: number | null;
  connectedLoadKw: number | null;
  loadType: string | null;
  tariffCategoryCode: string | null;
  communicationType: string | null;
  paymentMode: string | null;
  isNetMeter: boolean | null;
  billDay: number | null;
  billCycle: string | null;
}

interface CustomerResponse {
  id: string;
  accountNumber: string;
  name: string;
}

const columns: ReportColumn<ConsumerMasterRow>[] = [
  { key: "accountNumber", label: "Account Number", render: (r) => r.accountNumber },
  { key: "name", label: "Consumer Name", render: (r) => r.name },
  { key: "rrNumber", label: "RR Number", render: (r) => r.rrNumber ?? "—" },
  { key: "meterNumber", label: "Meter Number", render: (r) => r.meterNumber ?? "—" },
  { key: "dtrCode", label: "DTR", render: (r) => r.dtrCode ?? "—" },
  { key: "feederCode", label: "Feeder", render: (r) => r.feederCode ?? "—" },
  { key: "substationCode", label: "Substation", render: (r) => r.substationCode ?? "—" },
  { key: "zone", label: "Zone", render: (r) => r.zone ?? "—" },
  { key: "circle", label: "Circle", render: (r) => r.circle ?? "—" },
  { key: "division", label: "Division", render: (r) => r.division ?? "—" },
  { key: "subDivision", label: "Sub Division", render: (r) => r.subDivision ?? "—" },
  { key: "address", label: "Address", render: (r) => r.address },
  { key: "mobileNumber", label: "Mobile Number", render: (r) => r.mobileNumber ?? "—" },
  { key: "connectionStatus", label: "Connection Status", render: (r) => r.connectionStatus ?? "—" },
  { key: "serviceDate", label: "Service Date", render: (r) => r.serviceDate ?? "—" },
  { key: "sanctionedLoadKw", label: "Sanctioned Load (kW)", align: "right", render: (r) => r.sanctionedLoadKw ?? "—" },
  { key: "contractDemandKva", label: "Contract Demand (kVA)", align: "right", render: (r) => r.contractDemandKva ?? "—" },
  { key: "connectedLoadKw", label: "Connected Load (kW)", align: "right", render: (r) => r.connectedLoadKw ?? "—" },
  { key: "loadType", label: "Load Type", render: (r) => r.loadType ?? "—" },
  { key: "tariffCategoryCode", label: "Tariff Category", render: (r) => r.tariffCategoryCode ?? "—" },
  { key: "communicationType", label: "Communication", render: (r) => r.communicationType ?? "—" },
  { key: "paymentMode", label: "Payment Mode", render: (r) => r.paymentMode ?? "—" },
  { key: "isNetMeter", label: "Net Meter", render: (r) => (r.isNetMeter === null ? "—" : r.isNetMeter ? "Yes" : "No") },
  { key: "billDay", label: "Bill Day", align: "right", render: (r) => r.billDay ?? "—" },
  { key: "billCycle", label: "Bill Cycle", render: (r) => r.billCycle ?? "—" },
];

export default function ConsumersPage() {
  const queryClient = useQueryClient();
  const [accountNumber, setAccountNumber] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const createCustomer = useMutation({
    mutationFn: () => apiClient.post<CustomerResponse>("/api/v1/customers", { accountNumber, name }),
    onSuccess: () => {
      setAccountNumber("");
      setName("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["report", "/api/v1/customers/master"] });
    },
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Failed to create consumer."),
  });

  return (
    <Box>
      <PageHeader icon={<GroupsOutlinedIcon fontSize="small" />} title="Consumer" />

      <Paper variant="outlined" sx={{ p: 3, mb: 3 }}>
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

      <MasterDataListView<ConsumerMasterRow>
        title="Consumer Master Data"
        endpoint="/api/v1/customers/master"
        filenamePrefix="MDMS_Consumers"
        columns={columns}
        rowKey={(r) => r.id}
        searchPlaceholder="Account number, name, RR number, or meter number"
      />
    </Box>
  );
}
