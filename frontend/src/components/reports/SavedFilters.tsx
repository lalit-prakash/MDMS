"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  ListItemText,
  Menu,
  MenuItem,
  TextField,
  Alert,
} from "@mui/material";
import BookmarkBorderOutlinedIcon from "@mui/icons-material/BookmarkBorderOutlined";
import BookmarkAddOutlinedIcon from "@mui/icons-material/BookmarkAddOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { apiClient, ApiError } from "@/lib/apiClient";

interface SavedFilterResponse {
  id: string;
  screen: string;
  name: string;
  values: Record<string, string>;
}

/**
 * Per-user saved filter sets for one list screen, persisted server-side (SavedFiltersController)
 * so they follow the user across browsers. `current` is the screen's currently *applied* filter
 * values (empty ones omitted); `onLoad` receives a saved set to re-apply.
 */
export function SavedFilters({
  screen,
  current,
  onLoad,
}: {
  screen: string;
  current: Record<string, string>;
  onLoad: (values: Record<string, string>) => void;
}) {
  const queryClient = useQueryClient();
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["saved-filters", screen],
    queryFn: () => apiClient.get<SavedFilterResponse[]>(`/api/v1/saved-filters?screen=${encodeURIComponent(screen)}`),
  });

  const save = useMutation({
    mutationFn: () => apiClient.post("/api/v1/saved-filters", { screen, name, values: current }),
    onSuccess: () => {
      setDialogOpen(false);
      setName("");
      setError(null);
      queryClient.invalidateQueries({ queryKey: ["saved-filters", screen] });
    },
    onError: (err: unknown) => setError(err instanceof ApiError ? err.body || err.message : "Failed to save."),
  });

  const remove = useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/v1/saved-filters/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["saved-filters", screen] }),
  });

  const hasCurrent = Object.keys(current).length > 0;
  const saved = query.data ?? [];

  return (
    <>
      <Button size="small" startIcon={<BookmarkBorderOutlinedIcon fontSize="small" />} onClick={(e) => setMenuAnchor(e.currentTarget)}>
        Saved Filters{saved.length > 0 ? ` (${saved.length})` : ""}
      </Button>
      <Button
        size="small"
        startIcon={<BookmarkAddOutlinedIcon fontSize="small" />}
        disabled={!hasCurrent}
        title={hasCurrent ? "Save the currently applied filters" : "Apply at least one filter first"}
        onClick={() => setDialogOpen(true)}
      >
        Save
      </Button>

      <Menu anchorEl={menuAnchor} open={!!menuAnchor} onClose={() => setMenuAnchor(null)}>
        {saved.length === 0 && <MenuItem disabled>No saved filters yet</MenuItem>}
        {saved.map((f) => (
          <MenuItem
            key={f.id}
            dense
            onClick={() => {
              onLoad(f.values);
              setMenuAnchor(null);
            }}
          >
            <ListItemText primary={f.name} />
            <IconButton
              size="small"
              aria-label={`Delete saved filter ${f.name}`}
              onClick={(e) => {
                e.stopPropagation();
                remove.mutate(f.id);
              }}
            >
              <DeleteOutlineOutlinedIcon fontSize="small" />
            </IconButton>
          </MenuItem>
        ))}
      </Menu>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Save current filters</DialogTitle>
        <DialogContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}
          <TextField autoFocus fullWidth size="small" label="Name" value={name} onChange={(e) => setName(e.target.value)} sx={{ mt: 1 }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button variant="contained" disabled={!name.trim() || save.isPending} onClick={() => save.mutate()}>
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
