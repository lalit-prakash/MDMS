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
  Grid,
} from "@mui/material";
import { apiClient, ApiError } from "@/lib/apiClient";
import { TariffCategory, HierarchyNode, HierarchyNodeType, OrgUnit, OrgUnitType } from "@/lib/types";

function ErrorAlert({ error, onClose }: { error: string | null; onClose: () => void }) {
  if (!error) return null;
  return (
    <Alert severity="error" sx={{ mb: 2 }} onClose={onClose}>
      {error}
    </Alert>
  );
}

function TariffCategoriesSection() {
  const queryClient = useQueryClient();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["tariff-categories"],
    queryFn: () => apiClient.get<TariffCategory[]>("/api/v1/config/tariff-categories"),
  });

  const create = useMutation({
    mutationFn: () =>
      apiClient.post<TariffCategory>("/api/v1/config/tariff-categories", { code, name, description: null }),
    onSuccess: () => {
      setCode("");
      setName("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["tariff-categories"] });
    },
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Failed."),
  });

  return (
    <Box sx={{ mb: 5 }}>
      <Typography variant="h6" sx={{ mb: 1 }}>Tariff categories</Typography>
      <Paper variant="outlined" sx={{ p: 3, mb: 2 }}>
        <ErrorAlert error={error} onClose={() => setError(null)} />
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" } }}>
          <TextField label="Code" size="small" value={code} onChange={(e) => setCode(e.target.value)} />
          <TextField label="Name" size="small" value={name} onChange={(e) => setName(e.target.value)} />
          <Button variant="contained" disabled={!code || !name} onClick={() => create.mutate()}>Add</Button>
        </Stack>
      </Paper>
      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow><TableCell>Code</TableCell><TableCell>Name</TableCell></TableRow>
          </TableHead>
          <TableBody>
            {query.data?.length === 0 && <TableRow><TableCell colSpan={2}>None yet.</TableCell></TableRow>}
            {query.data?.map((c) => (
              <TableRow key={c.id} hover><TableCell>{c.code}</TableCell><TableCell>{c.name}</TableCell></TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}

function HierarchySection() {
  const queryClient = useQueryClient();
  const [nodeType, setNodeType] = useState<HierarchyNodeType>("Substation");
  const [parentId, setParentId] = useState("");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["hierarchy"],
    queryFn: () => apiClient.get<HierarchyNode[]>("/api/v1/config/hierarchy"),
  });

  const create = useMutation({
    mutationFn: () =>
      apiClient.post<HierarchyNode>("/api/v1/config/hierarchy", {
        nodeType,
        parentId: nodeType === "Substation" ? null : parentId || null,
        code,
        name,
      }),
    onSuccess: () => {
      setCode("");
      setName("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["hierarchy"] });
    },
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Failed."),
  });

  return (
    <Box sx={{ mb: 5 }}>
      <Typography variant="h6" sx={{ mb: 1 }}>Electrical hierarchy (Substation → Feeder → DT)</Typography>
      <Paper variant="outlined" sx={{ p: 3, mb: 2 }}>
        <ErrorAlert error={error} onClose={() => setError(null)} />
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, flexWrap: "wrap" }}>
          <TextField select label="Level" size="small" value={nodeType} onChange={(e) => setNodeType(e.target.value as HierarchyNodeType)} sx={{ minWidth: 160 }}>
            <MenuItem value="Substation">Substation</MenuItem>
            <MenuItem value="Feeder">Feeder</MenuItem>
            <MenuItem value="DistributionTransformer">Distribution Transformer</MenuItem>
          </TextField>
          {nodeType !== "Substation" && (
            <TextField select label="Parent" size="small" value={parentId} onChange={(e) => setParentId(e.target.value)} sx={{ minWidth: 220 }}>
              {query.data?.map((n) => (
                <MenuItem key={n.id} value={n.id}>{n.nodeType}: {n.name}</MenuItem>
              ))}
            </TextField>
          )}
          <TextField label="Code" size="small" value={code} onChange={(e) => setCode(e.target.value)} />
          <TextField label="Name" size="small" value={name} onChange={(e) => setName(e.target.value)} />
          <Button variant="contained" disabled={!code || !name} onClick={() => create.mutate()}>Add</Button>
        </Stack>
      </Paper>
      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow><TableCell>Level</TableCell><TableCell>Code</TableCell><TableCell>Name</TableCell></TableRow>
          </TableHead>
          <TableBody>
            {query.data?.length === 0 && <TableRow><TableCell colSpan={3}>None yet.</TableCell></TableRow>}
            {query.data?.map((n) => (
              <TableRow key={n.id} hover><TableCell>{n.nodeType}</TableCell><TableCell>{n.code}</TableCell><TableCell>{n.name}</TableCell></TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}

function OrgUnitsSection() {
  const queryClient = useQueryClient();
  const [unitType, setUnitType] = useState<OrgUnitType>("Zone");
  const [parentId, setParentId] = useState("");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["org-units"],
    queryFn: () => apiClient.get<OrgUnit[]>("/api/v1/config/org-units"),
  });

  const create = useMutation({
    mutationFn: () =>
      apiClient.post<OrgUnit>("/api/v1/config/org-units", {
        unitType,
        parentId: unitType === "Zone" ? null : parentId || null,
        code,
        name,
      }),
    onSuccess: () => {
      setCode("");
      setName("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["org-units"] });
    },
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Failed."),
  });

  return (
    <Box>
      <Typography variant="h6" sx={{ mb: 1 }}>
        Organizational hierarchy (Zone → Circle → Division → Sub Division → Section)
      </Typography>
      <Paper variant="outlined" sx={{ p: 3, mb: 2 }}>
        <ErrorAlert error={error} onClose={() => setError(null)} />
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, flexWrap: "wrap" }}>
          <TextField select label="Level" size="small" value={unitType} onChange={(e) => setUnitType(e.target.value as OrgUnitType)} sx={{ minWidth: 160 }}>
            <MenuItem value="Zone">Zone</MenuItem>
            <MenuItem value="Circle">Circle</MenuItem>
            <MenuItem value="Division">Division</MenuItem>
            <MenuItem value="SubDivision">Sub Division</MenuItem>
            <MenuItem value="Section">Section</MenuItem>
          </TextField>
          {unitType !== "Zone" && (
            <TextField select label="Parent" size="small" value={parentId} onChange={(e) => setParentId(e.target.value)} sx={{ minWidth: 220 }}>
              {query.data?.map((u) => (
                <MenuItem key={u.id} value={u.id}>{u.unitType}: {u.name}</MenuItem>
              ))}
            </TextField>
          )}
          <TextField label="Code" size="small" value={code} onChange={(e) => setCode(e.target.value)} />
          <TextField label="Name" size="small" value={name} onChange={(e) => setName(e.target.value)} />
          <Button variant="contained" disabled={!code || !name} onClick={() => create.mutate()}>Add</Button>
        </Stack>
      </Paper>
      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow><TableCell>Level</TableCell><TableCell>Code</TableCell><TableCell>Name</TableCell></TableRow>
          </TableHead>
          <TableBody>
            {query.data?.length === 0 && <TableRow><TableCell colSpan={3}>None yet.</TableCell></TableRow>}
            {query.data?.map((u) => (
              <TableRow key={u.id} hover><TableCell>{u.unitType}</TableCell><TableCell>{u.code}</TableCell><TableCell>{u.name}</TableCell></TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}

export default function ConfigPage() {
  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ fontWeight: 600 }}>
        Config
      </Typography>
      <Grid container spacing={4}>
        <Grid size={12}>
          <TariffCategoriesSection />
        </Grid>
        <Grid size={12}>
          <HierarchySection />
        </Grid>
        <Grid size={12}>
          <OrgUnitsSection />
        </Grid>
      </Grid>
    </Box>
  );
}
