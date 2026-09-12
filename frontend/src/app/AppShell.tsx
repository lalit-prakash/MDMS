"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { AppBar, Toolbar, Typography, Box, Tabs, Tab, Container } from "@mui/material";

const MODULES = [
  { label: "Home", href: "/" },
  { label: "Meters", href: "/meters" },
  { label: "Meter Data", href: "/meter-data" },
  { label: "VEE", href: "/vee" },
  { label: "Config", href: "/config" },
  { label: "Users", href: "/users" },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const activeIndex = Math.max(
    0,
    MODULES.findIndex((m) => (m.href === "/" ? pathname === "/" : pathname.startsWith(m.href)))
  );

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "#f4f6f8" }}>
      <AppBar position="static" color="primary" elevation={0}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 0, mr: 4, fontWeight: 600 }}>
            MDMS
          </Typography>
          <Tabs
            value={activeIndex}
            textColor="inherit"
            indicatorColor="secondary"
            sx={{ flexGrow: 1 }}
          >
            {MODULES.map((m) => (
              <Tab
                key={m.href}
                label={m.label}
                component={Link}
                href={m.href}
                sx={{ color: "inherit", minWidth: 0 }}
              />
            ))}
          </Tabs>
        </Toolbar>
      </AppBar>
      <Container maxWidth="lg" sx={{ py: 4 }}>
        {children}
      </Container>
    </Box>
  );
}
