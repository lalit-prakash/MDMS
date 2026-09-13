# MDMS Frontend

Next.js (App Router) + TypeScript + MUI v9 + TanStack Query frontend for the MDMS backend.

**Status**: one page per backend module that has one (Meters, Meter Data, VEE, Config, Users,
Energy Audit, Complaints, Revenue Protection, Prepaid — meter inventory/installation QC is
backend-only so far), a tile-based module hub as the home page (no role-based landing yet — see
backend's "Not yet built"), and a full light/dark design system (see below). No authentication
exists on either side yet, so every page is visible to everyone and every API call is
unauthenticated.

## Stack

- Next.js 16 (App Router), TypeScript, ESLint
- MUI (Material UI) v9 — note: v9 removed several convenience style props kept in earlier
  versions (e.g. `Typography`'s `fontWeight`, `Stack`'s `alignItems`/`justifyContent`/`flexWrap`
  as direct props, and the bare `"...Outline"` icon variants under `@mui/icons-material` — only
  theme-suffixed `...OutlineOutlined`/`...Rounded`/etc. ship now) in favor of `sx`-only styling.
  It also replaced per-color-per-variant `styleOverrides` keys (e.g. `containedPrimary`) with the
  `variants: [{ props, style }]` API — see `src/lib/theme/muiTheme.ts`. If upgrading MUI further,
  re-check all of this.
- TanStack Query v5 for all server data fetching/mutation
- ECharts (`echarts` + `echarts-for-react`) — installed, not yet used by any page (no
  load-curve/trend visualization has been built yet)

## Design system

Implements [`docs/MDMS_Complete_Color_Typography_Light_Dark.txt`](docs/MDMS_Complete_Color_Typography_Light_Dark.txt) (v2.0) in full: light mode default,
a fully-designed dark mode (not an inversion), Roboto Flex/Roboto Mono/Noto Sans typography, and
the spec's surface-elevation/spacing/radius scales.

- **`src/app/globals.css`** — the single token source of truth: `[data-theme="light"]` /
  `[data-theme="dark"]` CSS custom properties (`--color-*`, `--space-*`, `--radius-*`,
  `--font-*`, badge/table/button/chart tokens), copied verbatim from the spec doc. If a value here
  and the spec doc ever disagree, that's a bug — fix whichever is wrong, don't let them drift.
- **`src/lib/theme/ThemeModeContext.tsx`** — owns the persisted light/dark choice (`localStorage`,
  falling back to `prefers-color-scheme` on first visit) and stamps `[data-theme]` on `<html>`.
  `src/app/layout.tsx` also runs a `next/script` (`beforeInteractive`) that stamps the same
  attribute synchronously before first paint, so returning dark-mode users never see a light
  flash — the two must stay in agreement (same storage key, same fallback logic).
- **`src/lib/theme/muiTheme.ts`** — bridges those CSS variables into MUI's `createTheme`. Nearly
  every value is a `var(--x)` string; the *exception* is `palette.{primary,secondary,success,
  warning,error,info}.main`, `palette.text.*`, `palette.background.*`, and `palette.divider` —
  MUI runs `alpha()`/`getContrastText()`/`decomposeColor()` against these at render time (Chip
  and OutlinedInput borders, elevation overlays, hover states, ...), which throws on a `var()`
  string (MUI error #9) the moment such a component actually renders, even though `createTheme()`
  itself accepts it happily. Those specific fields are literal per-mode hex, hand-mirrored from
  the same spec values — documented at the top of the file, and the one deliberate exception to
  "one token source of truth."
- **`src/components/StatusBadge.tsx`** — every status/quality/source value in the app renders
  through this: a semantic-category badge (success/warning/error/info/neutral) with a color, an
  icon, *and* the exact text label — never color alone (spec §36/§7). Replaced the old
  `QualityChip`.
- **`src/components/MorphingStat.tsx`** — a KPI stat card whose number cross-fades via the View
  Transitions API when a TanStack Query refetch changes it, instead of snapping (used on Energy
  Audit and Prepaid).
- **`src/app/AppShell.tsx`** — the dark sidebar (brand-consistent across both themes per spec
  §21) + light/dark header + theme toggle, replacing the old top-tabs layout.

### View Transitions

`src/lib/viewTransition.ts` wraps the **native** `document.startViewTransition` API only — not
React's `<ViewTransition>` or Next's `experimental.viewTransition` flag, both still
experimental/unstable as of Next 16. Used for: sidebar navigation (cross-fade between page
shells, `navigateWithViewTransition`), the sidebar's active-item indicator (named
`sidebar-active-indicator`), and KPI card value morphs (`MorphingStat`, named per-card so no two
share a `view-transition-name` in one snapshot).

Falls back to a plain, instant update with zero visual difference — same code path — whenever:
the API is unsupported (non-Chromium browsers today), `prefers-reduced-motion` is set, or
`document.visibilityState !== "visible"` (the browser throws `InvalidStateError` calling
`startViewTransition` on a hidden/backgrounded document — hit and fixed live during this work).
Every transition's `ready`/`finished` promise rejections are swallowed for the same reason: a
transition can still fail after starting (a duplicate name elsewhere, the tab backgrounding
mid-transition) and must never surface as an unhandled rejection or block the underlying update,
which has already applied by then.

Verified in Chromium (this project's dev browser). Not tested in Safari/Firefox — the fallback
path above is what those get today regardless, since neither ships `startViewTransition` yet.

**Not done from the original design brief**: a table-row-expands-into-detail shared-element
transition (e.g. a meter row → detail drawer). The brief explicitly prohibits new frontend API
calls, and the only sensible instance of this pattern (`Meters` → per-meter detail) would call
`GET /api/v1/meters/{id}`, which the backend exposes but this frontend has never called — adding
that call would cross the brief's own line, so it's left out rather than adding data-fetching to
satisfy an animation.

## Getting started

```bash
cd frontend
npm install
cp .env.local.example .env.local   # set NEXT_PUBLIC_API_BASE_URL if the backend isn't on :5004
npm run dev
```

Open http://localhost:3000. The backend (`../backend`) must be running for any page's data to
load — without it, each page shows a plain "could not reach the backend API" warning rather than
crashing.

```bash
npm run build   # production build + typecheck
npm run lint    # ESLint
```

## Structure

```
src/
  app/
    globals.css          — design-system token source of truth (light/dark CSS custom properties)
    layout.tsx            — fonts (Roboto Flex/Mono, Noto Sans), pre-hydration theme-init script
    providers.tsx          — QueryClientProvider + ThemeModeProvider + MUI ThemeProvider bridge
    AppShell.tsx           — sidebar + header + theme toggle, shared by every page
    page.tsx               — home: tile-based module hub
    meters/page.tsx         — meter list + registration form
    meter-data/page.tsx     — LS intervals, DLPs, data-quality holds
    vee/page.tsx            — out-of-range thresholds, missing-interval estimation, audit trail
    config/page.tsx         — tariff categories, electrical hierarchy, organizational hierarchy
    users/page.tsx          — user list + creation (RBAC data, not enforcement)
    energy-audit/page.tsx   — network energy readings + transparent balance calculation
    complaints/page.tsx     — complaint tickets with SLA tracking and inline transitions
    revenue-protection/page.tsx — risk signals + investigation lead workflow
    prepaid/page.tsx        — wallet balance, recharge, daily billing, connect/disconnect
  components/
    StatusBadge.tsx        — color + icon + text status indicator, used everywhere
    MorphingStat.tsx        — a KPI card whose value cross-fades on refetch
  lib/
    apiClient.ts            — thin unauthenticated fetch wrapper
    types.ts, complaints.ts, revenueProtection.ts, prepaid.ts — hand-maintained types mirroring the backend's C# DTOs
    theme/ThemeModeContext.tsx, theme/muiTheme.ts — the design-system plumbing (see above)
    viewTransition.ts       — native View Transitions helpers with mandatory silent fallback
```

## Not yet built

- Authentication / role-based landing pages (backend has no auth to integrate against yet).
- Load-curve/trend charts (ECharts is installed but unused).
- A meter inventory/installation-QC page (backend-only module).
- A generated/shared API schema — `src/lib/*.ts` types are hand-maintained against the backend's
  C# types and will drift if not updated alongside backend changes.
- Pagination on any list (all lists rely on the backend's fixed caps, e.g. 500 rows).
- Non-Chromium View Transitions (Safari/Firefox get the instant fallback, untested beyond that).
- A table-row-to-detail-drawer shared-element transition (see "View Transitions" above for why).
