"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider, createTheme, CssBaseline } from "@mui/material";
import { useState } from "react";

// Shared color language for data-quality/status across the app — one place to change, not
// per-screen constants. Matches the spec's own instruction to define this as shared tokens.
export const qualityColor = {
  Valid: "#2e7d32",
  Received: "#2e7d32",
  Estimated: "#ed6c02",
  Edited: "#0288d1",
  Calculated: "#7b1fa2",
  NegativeConsumption: "#d32f2f",
  OutOfRange: "#ed6c02",
  Missing: "#9e9e9e",
  Suspect: "#d32f2f",
} as const;

const theme = createTheme({
  palette: {
    mode: "light",
    primary: { main: "#0f5fa8" },
  },
  shape: { borderRadius: 8 },
});

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: { queries: { staleTime: 30_000, retry: 1 } },
  }));

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </ThemeProvider>
    </QueryClientProvider>
  );
}
