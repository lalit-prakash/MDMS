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
import { MdmsUser, UserRole } from "@/lib/types";

const ROLES: UserRole[] = [
  "Admin",
  "ItManager",
  "Nomc",
  "Supervisor",
  "QualityIncharge",
  "OmSupervisor",
  "Installer",
  "OmExecutive",
  "Contractor",
  "UtilityManager",
  "ComplaintDesk",
  "StoreManager",
];

export default function UsersPage() {
  const queryClient = useQueryClient();
  const [username, setUsername] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [role, setRole] = useState<UserRole>("Supervisor");
  const [orgUnitId, setOrgUnitId] = useState("");
  const [error, setError] = useState<string | null>(null);

  const usersQuery = useQuery({
    queryKey: ["users"],
    queryFn: () => apiClient.get<MdmsUser[]>("/api/v1/users"),
  });

  const createUser = useMutation({
    mutationFn: () =>
      apiClient.post<MdmsUser>("/api/v1/users", {
        username,
        displayName,
        role,
        orgUnitId: role === "Admin" ? null : orgUnitId || null,
      }),
    onSuccess: () => {
      setUsername("");
      setDisplayName("");
      setOrgUnitId("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (err: unknown) =>
      setError(err instanceof ApiError ? err.body || err.message : "Failed to create user."),
  });

  return (
    <Box>
      <Typography variant="h4" gutterBottom sx={{ fontWeight: 600 }}>
        Users
      </Typography>
      <Alert severity="info" sx={{ mb: 3 }}>
        Identity data only — there is no login, credential, or permission-enforcement mechanism
        yet. This screen manages role/org-unit records, not authentication.
      </Alert>

      <Paper variant="outlined" sx={{ p: 3, mb: 3 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ alignItems: { sm: "center" }, flexWrap: "wrap" }}>
          <TextField label="Username" size="small" value={username} onChange={(e) => setUsername(e.target.value)} />
          <TextField label="Display name" size="small" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
          <TextField select label="Role" size="small" value={role} onChange={(e) => setRole(e.target.value as UserRole)} sx={{ minWidth: 180 }}>
            {ROLES.map((r) => (
              <MenuItem key={r} value={r}>{r}</MenuItem>
            ))}
          </TextField>
          {role !== "Admin" && (
            <TextField
              label="Org Unit ID"
              size="small"
              value={orgUnitId}
              onChange={(e) => setOrgUnitId(e.target.value)}
              helperText="Required for every role except Admin"
              sx={{ minWidth: 260 }}
            />
          )}
          <Button
            variant="contained"
            disabled={!username || !displayName || (role !== "Admin" && !orgUnitId) || createUser.isPending}
            onClick={() => createUser.mutate()}
          >
            Create
          </Button>
        </Stack>
      </Paper>

      <Paper variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Username</TableCell>
              <TableCell>Display name</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>Org Unit</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {usersQuery.data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>No users yet.</TableCell>
              </TableRow>
            )}
            {usersQuery.data?.map((u) => (
              <TableRow key={u.id} hover>
                <TableCell>{u.username}</TableCell>
                <TableCell>{u.displayName}</TableCell>
                <TableCell>{u.role}</TableCell>
                <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{u.orgUnitId ?? "—"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </Box>
  );
}
