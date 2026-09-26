"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { usePathname, useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  Autocomplete,
  Avatar,
  Badge,
  Box,
  Collapse,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  TextField,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import AccountTreeOutlinedIcon from "@mui/icons-material/AccountTreeOutlined";
import ExpandLessOutlinedIcon from "@mui/icons-material/ExpandLessOutlined";
import ExpandMoreOutlinedIcon from "@mui/icons-material/ExpandMoreOutlined";
import LightModeOutlinedIcon from "@mui/icons-material/LightModeOutlined";
import DarkModeOutlinedIcon from "@mui/icons-material/DarkModeOutlined";
import ChevronLeftOutlinedIcon from "@mui/icons-material/ChevronLeftOutlined";
import ChevronRightOutlinedIcon from "@mui/icons-material/ChevronRightOutlined";
import NotificationsNoneOutlinedIcon from "@mui/icons-material/NotificationsNoneOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
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
import GroupsOutlinedIcon from "@mui/icons-material/GroupsOutlined";
import AssessmentOutlinedIcon from "@mui/icons-material/AssessmentOutlined";
import HelpOutlineOutlinedIcon from "@mui/icons-material/HelpOutlineOutlined";
import CloudDownloadOutlinedIcon from "@mui/icons-material/CloudDownloadOutlined";
import { useThemeMode } from "@/lib/theme/ThemeModeContext";
import { navigateWithViewTransition } from "@/lib/viewTransition";
import { useAuth } from "@/lib/session/AuthContext";
import { TenantSwitcher } from "@/components/TenantSwitcher";
import { apiClient } from "@/lib/apiClient";
import { Complaint } from "@/lib/complaints";
import { RevenueProtectionLead } from "@/lib/revenueProtection";

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
  /** Rendered as a single collapsible "tree" entry (one parent + indented children) instead of a
   * flat labeled section — used for the Consumer/DTR/Feeder network hierarchy, where each child
   * shows only that entity's own master data (see NETWORK_TREE_PARENT below). */
  tree?: { parentLabel: string; parentIcon: ModuleLink["icon"] };
}

/** The Consumer/DTR/Feeder tree-like navigation: each leaf routes to its own master-data list
 * page (real HierarchyNode/Customer data — see NetworkController/CustomersController), not a
 * shared filtered view of one dataset. */
const NETWORK_TREE_ITEMS: ModuleLink[] = [
  { label: "Consumer", href: "/consumers", icon: GroupsOutlinedIcon },
  { label: "DTR", href: "/dtrs", icon: ElectricMeterOutlinedIcon },
  { label: "Feeder", href: "/feeders", icon: StorageOutlinedIcon },
];

// Grouping and icons mirror the approved design mockup — every entry here now has a real page
// behind it (Consumers, Energy Analytics, Reports, Settings, Help & Support included).
const GROUPS: ModuleGroup[] = [
  {
    label: "Overview",
    items: [
      { label: "Home", href: "/", icon: SpaceDashboardOutlinedIcon },
      { label: "Reports", href: "/reports", icon: AssessmentOutlinedIcon },
      { label: "Download Requests", href: "/downloads", icon: CloudDownloadOutlinedIcon },
    ],
  },
  {
    label: "Network",
    items: NETWORK_TREE_ITEMS,
    tree: { parentLabel: "Consumer / DTR / Feeder", parentIcon: AccountTreeOutlinedIcon },
  },
  {
    label: "Operations",
    items: [
      { label: "Meters", href: "/meters", icon: ElectricMeterOutlinedIcon },
      { label: "Meter Data", href: "/meter-data", icon: StorageOutlinedIcon },
      { label: "VEE & Data Quality", href: "/vee", icon: FactCheckOutlinedIcon },
    ],
  },
  {
    label: "Analytics",
    items: [
      { label: "Energy Analytics", href: "/energy-analytics", icon: InsightsOutlinedIcon },
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
      { label: "Configuration", href: "/config", icon: TuneOutlinedIcon },
      { label: "Users & Access", href: "/users", icon: GroupOutlinedIcon },
      { label: "Settings", href: "/settings", icon: TuneOutlinedIcon },
      { label: "Help & Support", href: "/help", icon: HelpOutlineOutlinedIcon },
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
  const networkGroupActive = NETWORK_TREE_ITEMS.some((m) => isActive(pathname, m.href));
  const [treeOpen, setTreeOpen] = useState<boolean>(true);

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
        {GROUPS.map((group) =>
          group.tree ? (
            <Box key={group.label} sx={{ mb: "var(--space-2)" }}>
              {(() => {
                const ParentIcon = group.tree.parentIcon;
                const parentButton = (
                  <ListItemButton
                    onClick={() => (collapsed ? onNavigate(group.items[0].href) : setTreeOpen((o) => !o))}
                    selected={networkGroupActive}
                    sx={{
                      borderRadius: "var(--radius-md)",
                      mb: "2px",
                      pl: collapsed ? "var(--space-3)" : "var(--space-4)",
                      justifyContent: collapsed ? "center" : "flex-start",
                      color: networkGroupActive ? "var(--color-sidebar-active-text)" : "var(--color-sidebar-text)",
                      "&.Mui-selected": { bgcolor: "var(--color-sidebar-active)" },
                      "&:hover": { bgcolor: "var(--color-sidebar-hover)" },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: collapsed ? 0 : 36, color: "var(--color-sidebar-text-secondary)" }}>
                      <ParentIcon fontSize="small" />
                    </ListItemIcon>
                    {!collapsed && (
                      <>
                        <ListItemText primary="Network" slotProps={{ primary: { sx: { fontSize: 14, fontWeight: 600 } } }} />
                        {treeOpen ? <ExpandLessOutlinedIcon fontSize="small" /> : <ExpandMoreOutlinedIcon fontSize="small" />}
                      </>
                    )}
                  </ListItemButton>
                );
                return collapsed ? (
                  <Tooltip title="Consumer / DTR / Feeder" placement="right">
                    {parentButton}
                  </Tooltip>
                ) : (
                  parentButton
                );
              })()}
              <Collapse in={collapsed || treeOpen}>
                {group.items.map((m) => {
                  const active = isActive(pathname, m.href);
                  const Icon = m.icon;
                  const child = (
                    <ListItemButton
                      key={m.href}
                      selected={active}
                      onClick={() => onNavigate(m.href)}
                      sx={{
                        position: "relative",
                        borderRadius: "var(--radius-md)",
                        mb: "2px",
                        pl: collapsed ? "var(--space-3)" : "var(--space-8)",
                        justifyContent: collapsed ? "center" : "flex-start",
                        color: active ? "var(--color-sidebar-active-text)" : "var(--color-sidebar-text)",
                        bgcolor: active ? "var(--color-sidebar-active)" : "transparent",
                        "&.Mui-selected": { bgcolor: "var(--color-sidebar-active)" },
                        "&:hover": { bgcolor: "var(--color-sidebar-hover)" },
                      }}
                    >
                      <ListItemIcon sx={{ minWidth: collapsed ? 0 : 28, color: active ? "var(--color-sidebar-active-text)" : "var(--color-sidebar-text-secondary)" }}>
                        <Icon fontSize="small" />
                      </ListItemIcon>
                      {!collapsed && (
                        <ListItemText primary={m.label} slotProps={{ primary: { sx: { fontSize: 13, fontWeight: active ? 600 : 500 } } }} />
                      )}
                    </ListItemButton>
                  );
                  return collapsed ? (
                    <Tooltip key={m.href} title={m.label} placement="right">
                      {child}
                    </Tooltip>
                  ) : (
                    child
                  );
                })}
              </Collapse>
            </Box>
          ) : (
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
          )
        )}
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

/** Bell badge count: a real sum of open complaints + open investigations, not a fabricated number. */
function useAlertCount() {
  const complaintsQuery = useQuery({
    queryKey: ["dashboard-complaints"],
    queryFn: () => apiClient.get<Complaint[]>("/api/v1/complaints"),
    retry: false,
  });
  const leadsQuery = useQuery({
    queryKey: ["dashboard-revenue-protection-leads"],
    queryFn: () => apiClient.get<RevenueProtectionLead[]>("/api/v1/revenue-protection/leads"),
    retry: false,
  });
  const openComplaints = complaintsQuery.data?.filter((c) => c.status !== "Resolved" && c.status !== "Closed").length ?? 0;
  const openLeads = leadsQuery.data?.filter((l) => l.status !== "Closed").length ?? 0;
  return openComplaints + openLeads;
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { mode, toggleMode } = useThemeMode();
  const { user, ready, logout } = useAuth();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [userMenuAnchor, setUserMenuAnchor] = useState<HTMLElement | null>(null);
  const isDesktop = useMediaQuery("(min-width:900px)");
  const alertCount = useAlertCount();

  const isLoginRoute = pathname === "/login";

  // Real auth (JWT access token + refresh cookie) — see lib/session/AuthContext.tsx. This gates
  // the UI's own navigation; the backend enforces the same requirement independently on every
  // controller endpoint, so this redirect is a UX convenience, not the actual security boundary.
  useEffect(() => {
    if (ready && !user && !isLoginRoute) {
      router.replace("/login");
    }
  }, [ready, user, isLoginRoute, router]);

  const currentModule = MODULES.find((m) => isActive(pathname, m.href));
  const sidebarWidth = collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH;

  const handleNavigate = (href: string) => {
    setMobileOpen(false);
    if (pathname === href) return;
    navigateWithViewTransition((h) => router.push(h), href);
  };

  if (isLoginRoute) {
    return <>{children}</>;
  }

  if (!ready || !user) {
    // Avoids flashing the full app shell for a signed-out visitor before the redirect above fires.
    return <Box sx={{ minHeight: "100vh", bgcolor: "var(--color-bg-app)" }} />;
  }

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
            gap: "var(--space-3)",
          }}
        >
          {!isDesktop && (
            <IconButton onClick={() => setMobileOpen(true)} sx={{ color: "var(--header-icon)" }}>
              <MenuIcon />
            </IconButton>
          )}

          {isDesktop ? (
            <Autocomplete
              size="small"
              options={MODULES}
              getOptionLabel={(m) => m.label}
              onChange={(_e, value) => value && handleNavigate(value.href)}
              sx={{ width: 320 }}
              renderInput={(params) => (
                <TextField
                  {...params}
                  placeholder="Search meters, consumers, complaints…"
                  slotProps={{
                    ...params.slotProps,
                    input: {
                      ...params.slotProps.input,
                      startAdornment: <SearchOutlinedIcon fontSize="small" sx={{ color: "var(--card-muted)", mr: 0.5 }} />,
                    },
                  }}
                />
              )}
            />
          ) : (
            <Typography sx={{ flex: 1, color: "var(--header-text)", fontSize: 14, fontWeight: 500 }}>
              {currentModule?.label ?? "MDMS"}
            </Typography>
          )}

          <Box sx={{ flex: 1 }} />

          <TenantSwitcher />

          <Tooltip title={alertCount > 0 ? `${alertCount} open items` : "No open items"}>
            <IconButton sx={{ color: "var(--header-icon)" }}>
              <Badge badgeContent={alertCount} color="error">
                <NotificationsNoneOutlinedIcon fontSize="small" />
              </Badge>
            </IconButton>
          </Tooltip>

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

          <Box
            onClick={(e) => setUserMenuAnchor(e.currentTarget)}
            sx={{ display: "flex", alignItems: "center", gap: 1, cursor: "pointer", pl: 1 }}
          >
            <Avatar sx={{ width: 32, height: 32, bgcolor: "var(--color-accent)", color: "#fff", fontSize: 14 }}>
              {user.displayName.slice(0, 1).toUpperCase()}
            </Avatar>
            {isDesktop && (
              <Box sx={{ lineHeight: 1.1 }}>
                <Typography sx={{ fontSize: 13, fontWeight: 600, color: "var(--header-text)" }}>{user.displayName}</Typography>
                <Typography sx={{ fontSize: 11, color: "var(--card-muted)" }}>{user.role}</Typography>
              </Box>
            )}
          </Box>
          <Menu anchorEl={userMenuAnchor} open={!!userMenuAnchor} onClose={() => setUserMenuAnchor(null)}>
            <MenuItem
              onClick={async () => {
                setUserMenuAnchor(null);
                await logout();
                // replace, not push: drops the authenticated page from history so a plain "back"
                // press can't return to it at all, independent of the bfcache fix in AuthContext.
                router.replace("/login");
              }}
            >
              Sign out
            </MenuItem>
          </Menu>
        </Toolbar>

        <Box component="main" sx={{ maxWidth: 1200, mx: "auto", px: "var(--space-6)", py: "var(--space-8)" }}>
          {children}
        </Box>
      </Box>
    </Box>
  );
}
