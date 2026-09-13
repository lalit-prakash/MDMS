"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import { useMemo, useState } from "react";
import { ThemeModeProvider, useThemeMode } from "@/lib/theme/ThemeModeContext";
import { buildMuiTheme } from "@/lib/theme/muiTheme";
import { AuthProvider } from "@/lib/session/AuthContext";

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
    // Emotion (MUI's styling engine) needs its own cache provider on the App Router: without it,
    // the client re-inserts <style> tags in a different order/insertion-point than the server
    // rendered, which React's hydration diff sees as a mismatched tree (a <style data-emotion>
    // node vs. a <div>) even though nothing about our own markup changed.
    <AppRouterCacheProvider options={{ key: "mui" }}>
      <QueryClientProvider client={queryClient}>
        <ThemeModeProvider>
          <MuiThemeBridge>
            <AuthProvider>{children}</AuthProvider>
          </MuiThemeBridge>
        </ThemeModeProvider>
      </QueryClientProvider>
    </AppRouterCacheProvider>
  );
}
