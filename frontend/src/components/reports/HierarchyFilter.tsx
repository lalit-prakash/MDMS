"use client";

import { useMemo } from "react";
import { MenuItem, TextField } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/apiClient";
import { OrgUnit, OrgUnitType } from "@/lib/types";

const LEVELS: { type: OrgUnitType; label: string }[] = [
  { type: "Region", label: "Region" },
  { type: "Zone", label: "Zone" },
  { type: "Circle", label: "Circle" },
  { type: "Division", label: "Division" },
  { type: "SubDivision", label: "Sub Division" },
  { type: "Section", label: "Section" },
];

/**
 * Real Region → Zone → Circle → Division → Sub Division → Section office filter, backed by the
 * actual OrgUnit tree ConfigController manages. One select per level; choosing a level narrows
 * every finer level's options to that unit's descendants, and the filter value reported upward is
 * always the finest unit chosen (the endpoints match anything at or beneath it). Levels a given
 * hierarchy doesn't use simply show no options.
 */
export function HierarchyFilter({ value, onChange }: { value: string; onChange: (orgUnitId: string) => void }) {
  const orgUnitsQuery = useQuery({
    queryKey: ["org-units", "all"],
    queryFn: () => apiClient.get<OrgUnit[]>("/api/v1/config/org-units"),
  });

  const units = useMemo(() => orgUnitsQuery.data ?? [], [orgUnitsQuery.data]);
  const byId = useMemo(() => new Map(units.map((u) => [u.id, u])), [units]);

  const isDescendantOf = (unit: OrgUnit, ancestorId: string) => {
    let current: OrgUnit | undefined = unit;
    while (current) {
      if (current.id === ancestorId) return true;
      current = current.parentId ? byId.get(current.parentId) : undefined;
    }
    return false;
  };

  // The selected unit and its ancestors, keyed by level type.
  const selectedByType = useMemo(() => {
    const map = new Map<OrgUnitType, OrgUnit>();
    let current = value ? byId.get(value) : undefined;
    while (current) {
      map.set(current.unitType, current);
      current = current.parentId ? byId.get(current.parentId) : undefined;
    }
    return map;
  }, [value, byId]);

  return (
    <>
      {LEVELS.map((level, i) => {
        // The nearest coarser level that has a selection scopes this level's options.
        const scopeAncestor = LEVELS.slice(0, i)
          .reverse()
          .map((l) => selectedByType.get(l.type))
          .find(Boolean);
        const options = units.filter((u) => u.unitType === level.type && (!scopeAncestor || isDescendantOf(u, scopeAncestor.id)));
        if (options.length === 0 && !selectedByType.get(level.type)) return null;

        return (
          <TextField
            key={level.type}
            size="small"
            select
            label={level.label}
            value={selectedByType.get(level.type)?.id ?? ""}
            onChange={(e) => onChange(e.target.value || scopeAncestor?.id || "")}
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
