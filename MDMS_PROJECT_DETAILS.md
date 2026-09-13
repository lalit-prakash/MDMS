# MDMS — Full Project Update

_Reflects the state of `main` after 11 merged PRs. Verified just before writing this: backend
build clean, **111/111 tests passing**; frontend `npm run build` and `npm run lint` clean._

---

## 1. What this project is now

MDMS has grown from a meter-data skeleton into a broad (if shallow in places) implementation of
the full functional spec you supplied: meter master data, measurement ingestion/VEE, DISCOM
hierarchy/reference config, energy audit, complaints, meter inventory + installation QC, revenue
protection, and a prepaid wallet — all in one modular-monolith backend, with a working frontend
covering every module except one.

Repository: `https://github.com/lalit-prakash/MDMS` (11 PRs merged: #1–#11)

---

## 2. Tech stack

| Layer | Choice |
|---|---|
| Backend | C# / ASP.NET Core Web API, **net8.0** (net10.0 SDK still not installed in this environment — TFM bump is mechanical whenever it is) |
| ORM | EF Core 8.x (explicitly pinned) |
| Database | PostgreSQL via Npgsql — schema verified via `migrations script`, never applied to a live DB in this environment (no credentials available here) |
| Backend tests | xUnit + EF Core InMemory — **111 passing** |
| Frontend | Next.js 16 (App Router) + TypeScript, MUI v9, TanStack Query v5, ECharts (installed, unused) |

---

## 3. Every module, what it does, and its honest gaps

### Meter master data (`MetersController`)
Meter registration, temporal install/replacement history (`MeterAssignment`), `ServicePoint` ↔
`Customer` ↔ `Tenant`. Solid, well-tested foundation everything else builds on.

### Meter data / VEE (`MeterDataController`, `VeeController`)
- LS (30-min) and DLP (daily) ingestion with explicit `Source`/`Quality` provenance.
- Negative-consumption detection → `DataQualityHold`.
- Out-of-range plausibility checks (`MeasurementRangeThreshold`, a `VeeRuleDefinition` subclass —
  designed so future rule types are new subclasses, not rewrites).
- Missing-interval detection + "average of surrounding periods" estimation, with every attempt
  (success or documented skip) recorded in `VeeExecutionRecord` — VEE's own audit trail.
- **Gap**: no IP (15-min) or BP (monthly) ingestion. No timestamp/boundary validation beyond
  interval-continuity. No DLP-side missing-day estimation.

### Config (`ConfigController`)
- `TariffCategory` — classification only, no rate/slab math.
- **Two separate hierarchies**, deliberately not conflated (a real bug I found and fixed
  mid-session): `HierarchyNode` (electrical: Substation→Feeder→DT, for energy audit) and
  `OrgUnit` (organizational: Zone→Circle→Division→SubDivision→Section, for RBAC/dashboards).

### Users / RBAC (`UsersController`)
`User` with all 12 spec roles and mandatory `OrgUnit` scoping (except Admin). **Data model
only — nothing enforces it.** No login, no tokens, no permission checks anywhere. Blocked on an
auth-package decision (JWT bearer vs. something else) I haven't made unilaterally.

### Energy audit (`EnergyAuditController`)
`NetworkEnergyReading` (entered manually — no feeder/DTR meter pipeline exists) vs. summed
downstream DLP consumption via `ServicePoint.DistributionTransformerNodeId`. Deliberately reports
the raw discrepancy **and** data-completeness separately rather than a fabricated real-loss vs.
coverage-loss split with no defensible formula.

### Complaints (`ComplaintsController`)
Open → Assigned → InProgress → Resolved → Closed, SLA due-time from a caller-supplied duration
(never hard-coded), closing requires having passed through Resolved first. **Gap**: no automated
ticket triggers (non-communicating meter, missing DP/BP, reconnection-SLA-breach) — every ticket
is raised through the API today.

### Meter inventory + installation QC (`MeterInventoryController`) — **no frontend page yet**
Two separate state machines: `MeterInventoryRecord` (Received→...→Active, plus a replacement
sub-flow) and `InstallationQualityCheck` (the spec's exact 3-level Contractor→Quality
Incharge→Utility Manager→RMS sync→MIS onboarding flow, with rejections landing at the correct
prior level).

### Revenue protection (`RevenueProtectionController`)
`RiskSignal` (caller-supplied weight — never a hard-coded table, per the spec's own warning about
weights needing utility calibration) feeding a `RevenueProtectionLead` through Detected→Scored→
Reviewed→Assigned→FieldInvestigation→FindingRecorded→ActionTaken→(optionally)RecoveryRecorded→
Closed. **Gap**: no automated signal detection — every signal is raised through the API.

### Prepaid (`PrepaidController`)
Ledger-based `PrepaidAccount`/`WalletTransaction` — balance only ever changes through a method
that returns the transaction recording it. **Idempotent** recharge (on a caller reference) and
daily billing (on customer+date). Daily billing sums DLP consumption and charges a **flat
caller-supplied rate — a deliberate placeholder, not a real tariff engine**. Disconnect/reconnect
are status flags only — **no HES/meter-command integration**, so "sent" and "confirmed" aren't
actually distinguished here yet, unlike the spec's own principle that they should be.

---

## 4. Frontend

9 pages, one per module except meter inventory/QC: Home (tile hub), Meters, Meter Data, VEE,
Config, Users, Energy Audit, Complaints, Revenue Protection, Prepaid. Shared quality/status color
language (`qualityColor`), thin unauthenticated API client, hand-maintained TypeScript types
mirroring the backend's C# DTOs (will drift if not updated alongside backend changes — no shared
schema generation).

Noted and worked around: MUI v9 removed `Typography.fontWeight` and `Stack`'s
`alignItems`/`justifyContent`/`flexWrap` as direct props (now `sx`-only) — every usage was moved
into `sx` rather than downgrading the package.

---

## 5. What's explicitly NOT built (by design, documented, not hidden)

- **Authentication/authorization enforcement** — the single biggest gap. Data model exists, zero
  enforcement.
- **A real tariff engine** — prepaid billing and `TariffCategory` don't connect to actual rate
  slabs/ToD/categories anywhere.
- **`wfm` module proper** — a work-order object tying installation/replacement to a contractor
  and installer end-to-end. Inventory + QC exist as separate state machines; nothing connects
  them yet.
- **Postpaid `billing` module** — bill determinant generation from validated interval data.
- **IP (15-min) and BP (monthly) profile ingestion** — only LS (30-min) and DLP (daily) exist.
- **HES/meter-command integration** — RC/DC and prepaid disconnect/reconnect are local status
  flags, never a dispatched-and-acknowledged physical command.
- **Automated triggers** — complaint auto-tickets and revenue-protection signal detection are
  both API-driven only; nothing in MDMS analyzes data on its own and raises something.
  Real feeder/DTR meter ingestion for energy audit (readings entered manually today).
- **Frontend page** for meter inventory/installation QC.

---

## 6. Process note

Every module this session was built on its own branch, verified (build + full test suite,
frontend build + lint), committed, pushed, and merged via PR — 11 PRs, all squash-merged into
`main`, all still verified-green as of this update.
