"use client";

import { useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { MenuItem, Select } from "@mui/material";
import CorporateFareOutlinedIcon from "@mui/icons-material/CorporateFareOutlined";
import { apiClient } from "@/lib/apiClient";
import { getSelectedTenantId, setSelectedTenantId } from "@/lib/session/tenantStore";

interface TenantOption {
  id: string;
  code: string;
  name: string;
  isCurrent: boolean;
  isHome: boolean;
}

/**
 * Header Organisation switcher. Lists the organisations the signed-in user can access and shows
 * the one the *server* says is active (not merely what is stored locally). Switching stores the
 * choice, then drops every cached query so all screens refetch against the new organisation's
 * data. Hidden when the user only has one organisation.
 */
export function TenantSwitcher() {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["tenants", "mine"],
    queryFn: () => apiClient.get<TenantOption[]>("/api/v1/tenants/mine"),
    staleTime: 60_000,
  });
  const tenants = query.data ?? [];
  const current = tenants.find((t) => t.isCurrent);

  // A stored choice the server no longer honours (e.g. another user signed in on this browser)
  // is discarded so the UI and the data always agree.
  useEffect(() => {
    const stored = getSelectedTenantId();
    if (query.data && stored && current && stored !== current.id) setSelectedTenantId(current.isHome ? null : current.id);
  }, [query.data, current]);

  if (tenants.length < 2 || !current) return null;

  return (
    <Select
      size="small"
      value={current.id}
      onChange={(e) => {
        const next = tenants.find((t) => t.id === e.target.value);
        if (!next) return;
        setSelectedTenantId(next.isHome ? null : next.id);
        queryClient.removeQueries();
        queryClient.invalidateQueries();
      }}
      startAdornment={<CorporateFareOutlinedIcon fontSize="small" sx={{ mr: 1, color: "var(--card-muted)" }} />}
      inputProps={{ "aria-label": "Organisation" }}
      sx={{ minWidth: 200, fontSize: 13 }}
    >
      {tenants.map((t) => (
        <MenuItem key={t.id} value={t.id} sx={{ fontSize: 13 }}>
          {t.name}
        </MenuItem>
      ))}
    </Select>
  );
}
