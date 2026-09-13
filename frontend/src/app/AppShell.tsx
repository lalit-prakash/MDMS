"use client";

import { useState } from "react";
import Image from "next/image";
import { usePathname, useRouter } from "next/navigation";
import {
  Box,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import LightModeOutlinedIcon from "@mui/icons-material/LightModeOutlined";
import DarkModeOutlinedIcon from "@mui/icons-material/DarkModeOutlined";
import ChevronLeftOutlinedIcon from "@mui/icons-material/ChevronLeftOutlined";
import ChevronRightOutlinedIcon from "@mui/icons-material/ChevronRightOutlined";
import SpaceDashboardOutlinedIcon from "@mui/icons-material/SpaceDashboardOutlined";
import ElectricMeterOutlinedIcon from "@mui/icons-material/ElectricMeterOutlined";
import StorageOutlinedIcon from "@mui/icons-material/StorageOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import InsightsOutlinedIcon from "@mui/icons-material/InsightsOutlined";
import ShieldOutlinedIcon from "@mui/icons-material/ShieldOutlined";
import SupportAgentOutlinedIcon from "@mui/icons-material/SupportAgentOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import { useThemeMode } from "@/lib/theme/ThemeModeContext";
import { navigateWithViewTransition } from "@/lib/viewTransition";

const SIDEBAR_WIDTH = 248;
const SIDEBAR_WIDTH_COLLAPSED = 72;

interface ModuleLink {
  label: string;
  href: string;
  icon: React.ComponentType<{ fontSize?: "small" | "inherit" | "medium" | "large" }>;
}

interface ModuleGroup {
  label: string;
  items: ModuleLink[];
}

// Grouping and icons mirror the approved design mockup. Only modules that actually exist in this
// frontend are listed — no "Consumers" / "Energy Analytics" placeholders for pages that aren't built.
const GROUPS: ModuleGroup[] = [
  { label: "Overview", items: [{ label: "Home", href: "/", icon: SpaceDashboardOutlinedIcon }] },
  {
    label: "Operations",
    items: [
      { label: "Meters", href: "/meters", icon: ElectricMeterOutlinedIcon },
      { label: "Meter Data", href: "/meter-data", icon: StorageOutlinedIcon },
      { label: "VEE", href: "/vee", icon: FactCheckOutlinedIcon },
    ],
  },
  {
    label: "Analytics",
    items: [
      { label: "Energy Audit", href: "/energy-audit", icon: InsightsOutlinedIcon },
      { label: "Revenue Protection", href: "/revenue-protection", icon: ShieldOutlinedIcon },
    ],
  },
  {
    label: "Customer",
    items: [
      { label: "Complaints", href: "/complaints", icon: SupportAgentOutlinedIcon },
      { label: "Prepaid", href: "/prepaid", icon: AccountBalanceWalletOutlinedIcon },
    ],
  },
  {
    label: "Administration",
    items: [
      { label: "Config", href: "/config", icon: TuneOutlinedIcon },
      { label: "Users", href: "/users", icon: GroupOutlinedIcon },
    ],
  },
];

const MODULES = GROUPS.flatMap((g) => g.items);

function isActive(pathname: string, href: string) {
  return href === "/" ? pathname === "/" : pathname.startsWith(href);
}

function SidebarContent({
  pathname,
  onNavigate,
  collapsed,
  onToggleCollapsed,
  showCollapseToggle,
}: {
  pathname: string;
  onNavigate: (href: string) => void;
  collapsed: boolean;
  onToggleCollapsed?: () => void;
  showCollapseToggle: boolean;
}) {
  return (
    <Box sx={{ height: "100%", display: "flex", flexDirection: "column", bgcolor: "var(--color-bg-sidebar)" }}>
      <Toolbar sx={{ gap: "var(--space-2)", px: collapsed ? "var(--space-3)" : "var(--space-5)", justifyContent: collapsed ? "center" : "flex-start" }}>
        <Image src="/mdms-mark.png" alt="" width={26} height={22} priority style={{ flexShrink: 0 }} />
        {!collapsed && (
          <Typography sx={{ color: "var(--color-sidebar-text)", fontWeight: 700, fontSize: 18, letterSpacing: 0.5 }}>
            MDMS
          </Typography>
        )}
      </Toolbar>

      <List sx={{ px: collapsed ? "var(--space-2)" : "var(--space-3)", py: "var(--space-2)", flex: 1, overflowY: "auto" }}>
        {GROUPS.map((group) => (
          <Box key={group.label} sx={{ mb: "var(--space-2)" }}>
            {!collapsed && (
              <Typography
                sx={{
                  color: "var(--color-sidebar-text-secondary)",
                  fontSize: 11,
                  fontWeight: 700,
                  letterSpacing: 1,
                  textTransform: "uppercase",
                  px: "var(--space-4)",
                  pt: "var(--space-3)",
                  pb: "var(--space-1)",
                }}
              >
                {group.label}
              </Typography>
            )}
            {group.items.map((m) => {
              const active = isActive(pathname, m.href);
              const Icon = m.icon;
              const button = (
                <ListItemButton
                  key={m.href}
                  selected={active}
                  onClick={() => onNavigate(m.href)}
                  sx={{
                    position: "relative",
                    borderRadius: "var(--radius-md)",
                    mb: "2px",
                    pl: collapsed ? "var(--space-3)" : "var(--space-4)",
                    justifyContent: collapsed ? "center" : "flex-start",
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
                  <ListItemIcon
                    sx={{
                      minWidth: collapsed ? 0 : 36,
                      color: active ? "var(--color-sidebar-active-text)" : "var(--color-sidebar-text-secondary)",
                    }}
                  >
                    <Icon fontSize="small" />
                  </ListItemIcon>
                  {!collapsed && (
                    <ListItemText
                      primary={m.label}
                      slotProps={{ primary: { sx: { fontSize: 14, fontWeight: active ? 600 : 500 } } }}
                    />
                  )}
                </ListItemButton>
              );
              return collapsed ? (
                <Tooltip key={m.href} title={m.label} placement="right">
                  {button}
                </Tooltip>
              ) : (
                button
              );
            })}
          </Box>
        ))}
      </List>

      {showCollapseToggle && (
        <Box sx={{ borderTop: "1px solid var(--color-sidebar-hover)", p: "var(--space-2)", display: "flex", justifyContent: collapsed ? "center" : "flex-end" }}>
          <IconButton
            onClick={onToggleCollapsed}
            aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
            size="small"
            sx={{ color: "var(--color-sidebar-text-secondary)" }}
          >
            {collapsed ? <ChevronRightOutlinedIcon fontSize="small" /> : <ChevronLeftOutlinedIcon fontSize="small" />}
          </IconButton>
        </Box>
      )}
    </Box>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { mode, toggleMode } = useThemeMode();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const isDesktop = useMediaQuery("(min-width:900px)");

  const currentModule = MODULES.find((m) => isActive(pathname, m.href));
  const sidebarWidth = collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH;

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
            width: sidebarWidth,
            transition: "width 150ms ease",
            zIndex: (t) => t.zIndex.drawer,
          }}
        >
          <SidebarContent
            pathname={pathname}
            onNavigate={handleNavigate}
            collapsed={collapsed}
            onToggleCollapsed={() => setCollapsed((c) => !c)}
            showCollapseToggle
          />
        </Box>
      ) : (
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{ "& .MuiDrawer-paper": { width: SIDEBAR_WIDTH, border: "none" } }}
        >
          <SidebarContent pathname={pathname} onNavigate={handleNavigate} collapsed={false} showCollapseToggle={false} />
        </Drawer>
      )}

      <Box sx={{ ml: isDesktop ? `${sidebarWidth}px` : 0, transition: "margin-left 150ms ease" }}>
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
