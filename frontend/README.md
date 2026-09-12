# MDMS Frontend

Next.js (App Router) + TypeScript + MUI + TanStack Query frontend for the MDMS backend.

**Status**: a first working vertical slice — one page per backend module that has one (Meters,
Meter Data, VEE, Config, Users, Energy Audit, Complaints, Revenue Protection, Prepaid — meter
inventory/installation QC is backend-only so far), a tile-based module hub as the home page (no role-based landing yet — see
backend's "Not yet built"), and a shared quality/status color language (`qualityColor` in
`src/app/providers.tsx`) used everywhere a `MeasurementQuality`/`MeasurementSource`/meter status
value is shown. No authentication exists on either side yet, so every page is visible to everyone
and every API call is unauthenticated.

## Stack

- Next.js 16 (App Router), TypeScript, ESLint
- MUI (Material UI) v9 — note: v9 removed several convenience style props kept in earlier
  versions (e.g. `Typography`'s `fontWeight`, `Stack`'s `alignItems`/`justifyContent`/`flexWrap`
  as direct props) in favor of the `sx` prop only. If upgrading MUI further, re-check this.
- TanStack Query v5 for all server data fetching/mutation
- ECharts (`echarts` + `echarts-for-react`) — installed, not yet used by any page (no
  load-curve/trend visualization has been built yet)

## Getting started

```bash
cd frontend
npm install
cp .env.local.example .env.local   # set NEXT_PUBLIC_API_BASE_URL if the backend isn't on :5004
npm run dev
```

Open http://localhost:3000. The backend (`../backend`) must be running for any page's data to
load — without it, each page shows a plain "could not reach the backend API" warning rather than
crashing (verified: every page renders correctly with the backend down, only the data tables
stay empty with that warning).

```bash
npm run build   # production build + typecheck
npm run lint    # ESLint
```

## Structure

```
src/
  app/
    providers.tsx      — QueryClientProvider + MUI ThemeProvider, shared qualityColor tokens
    AppShell.tsx        — top nav shared by every page
    page.tsx            — home: tile-based module hub
    meters/page.tsx      — meter list + registration form
    meter-data/page.tsx  — LS intervals, DLPs, data-quality holds (with clear action)
    vee/page.tsx         — out-of-range thresholds, missing-interval estimation run, audit trail
    config/page.tsx      — tariff categories, electrical hierarchy, organizational hierarchy
    users/page.tsx       — user list + creation (RBAC data, not enforcement)
  components/
    QualityChip.tsx      — shared color-coded chip for any quality/source/status value
  lib/
    apiClient.ts         — thin unauthenticated fetch wrapper
    types.ts              — TypeScript types mirroring the backend's C# DTOs (kept in sync by hand)
```

## Not yet built

- Authentication / role-based landing pages (backend has no auth to integrate against yet).
- Load-curve/trend charts (ECharts is installed but unused).
- Energy audit, revenue protection, prepaid, WFM, complaints screens — their backend modules
  don't exist yet either.
- A generated/shared API schema — `src/lib/types.ts` is hand-maintained against the backend's C#
  types and will drift if not updated alongside backend changes.
- Pagination on any list (all lists rely on the backend's fixed caps, e.g. 500 rows).
