"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { useMemo, useState } from "react";
import { ThemeModeProvider, useThemeMode } from "@/lib/theme/ThemeModeContext";
import { buildMuiTheme } from "@/lib/theme/muiTheme";

function MuiThemeBridge({ children }: { children: React.ReactNode }) {
  // Bridges our own ThemeModeContext (which owns the persisted light/dark choice and the
  // [data-theme] attribute) into MUI's ThemeProvider — one token source of truth
  // (globals.css custom properties), never a second hard-coded MUI palette.
  const { mode } = useThemeMode();
  const theme = useMemo(() => buildMuiTheme(mode), [mode]);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </ThemeProvider>
  );
}

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeModeProvider>
        <MuiThemeBridge>{children}</MuiThemeBridge>
      </ThemeModeProvider>
    </QueryClientProvider>
  );
}
