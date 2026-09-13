import { createTheme } from "@mui/material/styles";
import type { ThemeMode } from "./ThemeModeContext";

/**
 * Literal per-mode hex values for the palette slots MUI actually runs through its own color
 * math (`alpha()`/`getContrastText()`/hover-overlay calculations at render time — e.g. a
 * ListItemButton's `selected` state, an IconButton's hover ripple). MUI parses `palette.*.main`
 * as a real color for that; a `var(--x)` string throws (MUI error #9) the moment a component
 * actually renders with it, even though `createTheme()` itself accepts it happily.
 *
 * These are mirrored, not computed, from the same hex values in globals.css's `[data-theme]`
 * blocks (design spec §6/§7/§16) — the two must be kept in sync by hand if either changes.
 * Includes `text.primary/secondary/disabled`, since MUI's own OutlinedInput/Chip/etc. default
 * styles also run `alpha()` against `text.primary` for their default (unfocused/unselected)
 * border color. Every *other* theme value below (backgrounds, borders, and all our own component
 * styleOverrides) stays on `var(--x)`, since MUI just outputs those as plain CSS and never
 * decomposes them — that remains the token source of truth for everything but this one
 * unavoidable MUI limitation, which this file's comments flag everywhere it applies.
 */
interface PaletteHex {
  brandPrimary: string;
  accent: string;
  success: string;
  warning: string;
  error: string;
  info: string;
  textInverse: string;
  textPrimary: string;
  textSecondary: string;
  textDisabled: string;
  bgApp: string;
  bgSurface: string;
  borderDefault: string;
}

const PALETTE_HEX: Record<ThemeMode, PaletteHex> = {
  light: {
    brandPrimary: "#06141B",
    accent: "#0F9F91",
    success: "#16803C",
    warning: "#9A6700",
    error: "#CF222E",
    info: "#0969DA",
    textInverse: "#FFFFFF",
    textPrimary: "#11212D",
    textSecondary: "#4A5C6A",
    textDisabled: "#9BA8AB",
    bgApp: "#F5F7F8",
    bgSurface: "#FFFFFF",
    borderDefault: "#D9E0E3",
  },
  dark: {
    brandPrimary: "#CCD0CF",
    accent: "#2DD4BF",
    success: "#56D364",
    warning: "#E3B341",
    error: "#FF7B72",
    info: "#79C0FF",
    textInverse: "#06141B",
    textPrimary: "#CCD0CF",
    textSecondary: "#9BA8AB",
    textDisabled: "#56666D",
    bgApp: "#06141B",
    bgSurface: "#253745",
    borderDefault: "rgba(155, 168, 171, 0.18)",
  },
};

export function buildMuiTheme(mode: ThemeMode) {
  const hex = PALETTE_HEX[mode];

  return createTheme({
    palette: {
      mode,
      // Every field in this object is literal hex, mirrored from PALETTE_HEX above — not because
      // `palette` should be a second color system, but because MUI's own components run
      // `alpha()`/`decomposeColor()` against essentially any of these fields somewhere (Chip and
      // OutlinedInput borders against `text.primary`, elevation overlays against `background.*`,
      // `Divider`-adjacent components against `divider`) and a `var(--x)` string breaks the
      // moment such a component renders. `var(--x)` remains the real source of truth in
      // globals.css and is used everywhere else in this file (styleOverrides) and across the rest
      // of the app — this block is the one place forced to hold a mirrored copy.
      background: {
        default: hex.bgApp,
        paper: hex.bgSurface,
      },
      text: {
        primary: hex.textPrimary,
        secondary: hex.textSecondary,
        disabled: hex.textDisabled,
      },
      primary: { main: hex.brandPrimary, contrastText: hex.textInverse },
      secondary: { main: hex.accent, contrastText: hex.textInverse },
      success: { main: hex.success, contrastText: hex.textInverse },
      warning: { main: hex.warning, contrastText: hex.textInverse },
      error: { main: hex.error, contrastText: hex.textInverse },
      info: { main: hex.info, contrastText: hex.textInverse },
      divider: hex.borderDefault,
    },
    shape: {
      borderRadius: 8, // --radius-md; components override to --radius-lg/-xl where the spec calls for it
    },
    typography: {
      fontFamily: "var(--font-primary)",
      h1: { fontSize: "32px", lineHeight: 1.2, fontWeight: 600 }, // Page title
      h2: { fontSize: "24px", lineHeight: 1.25, fontWeight: 600 }, // Section
      h3: { fontSize: "18px", lineHeight: 1.35, fontWeight: 600 }, // Card title
      body1: { fontSize: "14px", lineHeight: 1.5, fontWeight: 400 },
      body2: { fontSize: "13px", lineHeight: 1.45, fontWeight: 400 }, // Body small
      caption: { fontSize: "12px", lineHeight: 1.4, fontWeight: 400 },
      button: { textTransform: "none", fontWeight: 500 },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            backgroundColor: "var(--color-bg-app)",
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundColor: "var(--color-bg-surface)",
            backgroundImage: "none",
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            backgroundColor: "var(--color-bg-surface)",
            border: "1px solid var(--color-border-default)",
            borderRadius: "var(--radius-lg)",
            boxShadow: "var(--shadow-card)",
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: "var(--radius-md)",
          },
        },
        // MUI v9 dropped the old `containedPrimary`/`outlinedSecondary`-style styleOverrides
        // keys in favor of `variants` (props + style pairs) — see the frontend README's MUI-v9
        // breaking-changes note.
        variants: [
          {
            props: { variant: "contained", color: "primary" },
            style: {
              backgroundColor: "var(--btn-primary-bg)",
              color: "var(--btn-primary-text)",
              "&:hover": { backgroundColor: "var(--btn-primary-hover)" },
              "&:active": { backgroundColor: "var(--btn-primary-active)" },
            },
          },
          {
            props: { variant: "outlined" },
            style: {
              backgroundColor: "var(--btn-secondary-bg)",
              color: "var(--btn-secondary-text)",
              borderColor: "var(--btn-secondary-border)",
              "&:hover": { backgroundColor: "var(--btn-secondary-hover)" },
            },
          },
          {
            props: { variant: "text" },
            style: {
              color: "var(--btn-ghost-text)",
              "&:hover": { backgroundColor: "var(--btn-ghost-hover)", color: "var(--btn-ghost-hover-text)" },
            },
          },
        ],
      },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            backgroundColor: "var(--input-bg)",
            borderRadius: "var(--radius-md)",
            "& fieldset": { borderColor: "var(--input-border)" },
            "&:hover fieldset": { borderColor: "var(--color-border-strong)" },
            "&.Mui-focused fieldset": { borderColor: "var(--color-focus)", boxShadow: "0 0 0 3px var(--color-focus-ring)" },
          },
          input: { color: "var(--input-text)" },
        },
      },
      MuiTableContainer: {
        styleOverrides: {
          root: { backgroundColor: "var(--table-container)" },
        },
      },
      MuiTableHead: {
        styleOverrides: {
          root: {
            "& .MuiTableCell-root": {
              backgroundColor: "var(--table-header)",
              color: "var(--table-header-text)",
              fontSize: "12px",
              fontWeight: 600,
              lineHeight: 1.4,
            },
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: {
            borderBottom: "1px solid var(--table-divider)",
            color: "var(--table-body)",
            fontSize: "13px",
          },
        },
      },
      MuiTableRow: {
        styleOverrides: {
          root: {
            "&:hover": { backgroundColor: "var(--table-hover)" },
          },
        },
      },
    },
  });
}
