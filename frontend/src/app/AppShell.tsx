"use client";

import { useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  Box,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Toolbar,
  Typography,
  useMediaQuery,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import LightModeOutlinedIcon from "@mui/icons-material/LightModeOutlined";
import DarkModeOutlinedIcon from "@mui/icons-material/DarkModeOutlined";
import BoltIcon from "@mui/icons-material/Bolt";
import { useThemeMode } from "@/lib/theme/ThemeModeContext";
import { navigateWithViewTransition } from "@/lib/viewTransition";

const SIDEBAR_WIDTH = 248;

const MODULES = [
  { label: "Home", href: "/" },
  { label: "Meters", href: "/meters" },
  { label: "Meter Data", href: "/meter-data" },
  { label: "VEE", href: "/vee" },
  { label: "Config", href: "/config" },
  { label: "Users", href: "/users" },
  { label: "Energy Audit", href: "/energy-audit" },
  { label: "Complaints", href: "/complaints" },
  { label: "Revenue Protection", href: "/revenue-protection" },
  { label: "Prepaid", href: "/prepaid" },
];

function isActive(pathname: string, href: string) {
  return href === "/" ? pathname === "/" : pathname.startsWith(href);
}

function SidebarContent({ pathname, onNavigate }: { pathname: string; onNavigate: (href: string) => void }) {
  return (
    <Box sx={{ height: "100%", display: "flex", flexDirection: "column", bgcolor: "var(--color-bg-sidebar)" }}>
      <Toolbar sx={{ gap: "var(--space-2)", px: "var(--space-5)" }}>
        <BoltIcon sx={{ color: "var(--icon-accent)" }} />
        <Typography sx={{ color: "var(--color-sidebar-text)", fontWeight: 600, fontSize: 18, letterSpacing: 0.5 }}>
          MDMS
        </Typography>
      </Toolbar>
      <List sx={{ px: "var(--space-3)", py: "var(--space-2)", flex: 1 }}>
        {MODULES.map((m) => {
          const active = isActive(pathname, m.href);
          return (
            <ListItemButton
              key={m.href}
              selected={active}
              onClick={() => onNavigate(m.href)}
              sx={{
                position: "relative",
                borderRadius: "var(--radius-md)",
                mb: "2px",
                pl: "var(--space-4)",
                color: active ? "var(--color-sidebar-active-text)" : "var(--color-sidebar-text)",
                bgcolor: active ? "var(--color-sidebar-active)" : "transparent",
                "&.Mui-selected": { bgcolor: "var(--color-sidebar-active)" },
                "&.Mui-selected:hover": { bgcolor: "var(--color-sidebar-active)" },
                "&:hover": { bgcolor: "var(--color-sidebar-hover)" },
              }}
            >
              {active && (
                <Box
                  style={{ viewTransitionName: "sidebar-active-indicator" } as React.CSSProperties}
                  sx={{
                    position: "absolute",
                    left: 0,
                    top: "20%",
                    bottom: "20%",
                    width: "3px",
                    borderRadius: "var(--radius-pill)",
                    bgcolor: "var(--color-sidebar-active-indicator)",
                  }}
                />
              )}
              <ListItemText
                primary={m.label}
                slotProps={{
                  primary: { sx: { fontSize: 14, fontWeight: active ? 600 : 500 } },
                }}
              />
            </ListItemButton>
          );
        })}
      </List>
    </Box>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { mode, toggleMode } = useThemeMode();
  const [mobileOpen, setMobileOpen] = useState(false);
  const isDesktop = useMediaQuery("(min-width:900px)");

  const currentModule = MODULES.find((m) => isActive(pathname, m.href));

  const handleNavigate = (href: string) => {
    setMobileOpen(false);
    if (pathname === href) return;
    navigateWithViewTransition((h) => router.push(h), href);
  };

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "var(--color-bg-app)" }}>
      {isDesktop ? (
        <Box
          component="nav"
          sx={{
            position: "fixed",
            insetBlock: 0,
            left: 0,
            width: SIDEBAR_WIDTH,
            zIndex: (t) => t.zIndex.drawer,
          }}
        >
          <SidebarContent pathname={pathname} onNavigate={handleNavigate} />
        </Box>
      ) : (
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{ "& .MuiDrawer-paper": { width: SIDEBAR_WIDTH, border: "none" } }}
        >
          <SidebarContent pathname={pathname} onNavigate={handleNavigate} />
        </Drawer>
      )}

      <Box sx={{ ml: isDesktop ? `${SIDEBAR_WIDTH}px` : 0 }}>
        <Toolbar
          sx={{
            position: "sticky",
            top: 0,
            zIndex: (t) => t.zIndex.appBar,
            bgcolor: "var(--color-bg-header)",
            borderBottom: "1px solid var(--header-border)",
            px: "var(--space-6)",
            gap: "var(--space-2)",
          }}
        >
          {!isDesktop && (
            <IconButton onClick={() => setMobileOpen(true)} sx={{ color: "var(--header-icon)" }}>
              <MenuIcon />
            </IconButton>
          )}
          <Typography sx={{ flex: 1, color: "var(--header-text)", fontSize: 14, fontWeight: 500 }}>
            {currentModule?.label ?? "MDMS"}
          </Typography>
          <IconButton
            onClick={toggleMode}
            aria-label="Toggle color mode"
            sx={{
              color: "var(--header-icon)",
              "&:hover": { color: "var(--header-icon-hover)", bgcolor: "var(--color-bg-surface-hover)" },
            }}
          >
            {mode === "light" ? <DarkModeOutlinedIcon fontSize="small" /> : <LightModeOutlinedIcon fontSize="small" />}
          </IconButton>
        </Toolbar>

        <Box component="main" sx={{ maxWidth: 1200, mx: "auto", px: "var(--space-6)", py: "var(--space-8)" }}>
          {children}
        </Box>
      </Box>
    </Box>
  );
}
