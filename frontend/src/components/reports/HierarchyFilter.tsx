"use client";

import { useMemo } from "react";
import { MenuItem, TextField } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/apiClient";
import { OrgUnit } from "@/lib/types";

/**
 * Real Zone → Circle → Division → Sub Division → Section office filter, backed by the actual
 * OrgUnit tree ConfigController manages (never a fabricated Organisation/Hierarchy dropdown) —
 * one cascading select per level, each populated only with children of the level above. Selecting
 * any level reports that OrgUnit's id upward; the meter-data endpoints resolve it down to the
 * concrete meters under it via the real Substation → Feeder → DT → ServicePoint chain.
 */
export function HierarchyFilter({ value, onChange }: { value: string; onChange: (orgUnitId: string) => void }) {
  const orgUnitsQuery = useQuery({
    queryKey: ["org-units", "all"],
    queryFn: () => apiClient.get<OrgUnit[]>("/api/v1/config/org-units"),
  });

  const units = orgUnitsQuery.data ?? [];
  const byId = useMemo(() => new Map(units.map((u) => [u.id, u])), [units]);

  // The chain of selected units from Zone down to whatever is currently selected, so each level's
  // dropdown can be scoped to children of the level above it.
  const selectedChain = useMemo(() => {
    const chain: OrgUnit[] = [];
    let current = value ? byId.get(value) : undefined;
    while (current) {
      chain.unshift(current);
      current = current.parentId ? byId.get(current.parentId) : undefined;
    }
    return chain;
  }, [value, byId]);

  const levels: { type: OrgUnit["unitType"]; label: string }[] = [
    { type: "Zone", label: "Zone" },
    { type: "Circle", label: "Circle" },
    { type: "Division", label: "Division" },
    { type: "SubDivision", label: "Sub Division" },
    { type: "Section", label: "Section" },
  ];

  return (
    <>
      {levels.map((level, i) => {
        const parentId = i === 0 ? null : selectedChain[i - 1]?.id ?? null;
        const options = units.filter((u) => u.unitType === level.type && (i === 0 ? true : u.parentId === parentId));
        const selectedAtLevel = selectedChain[i]?.id ?? "";
        if (i > 0 && !parentId) return null;

        return (
          <TextField
            key={level.type}
            size="small"
            select
            label={level.label}
            value={selectedAtLevel}
            onChange={(e) => onChange(e.target.value)}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">All</MenuItem>
            {options.map((u) => (
              <MenuItem key={u.id} value={u.id}>
                {u.name}
              </MenuItem>
            ))}
          </TextField>
        );
      })}
    </>
  );
}
