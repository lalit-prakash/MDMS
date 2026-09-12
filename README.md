# MDMS

Meter Data Management System — meter master data, measurement ingestion/VEE, load-profile data,
DISCOM reference/hierarchy configuration, energy audit, complaints, meter inventory/installation
QC, revenue protection, and a prepaid wallet — built as a standalone, self-contained platform for
smart-meter data and its downstream operational/commercial workflows.

**Scope**: this repository owns everything listed above end to end. It does **not** own real
tariff rate/slab calculation (the prepaid module's daily billing uses a deliberately simplified
flat rate × kWh placeholder, not a real tariff engine), postpaid billing determinant generation, a
`wfm` work-order module, or any HES/meter-command integration (so RC/DC here is a status flag with
confirmation gates, not a dispatched-and-acknowledged physical command). See "Not yet built" for
the full list of gaps.

**Current status**: backend domain + persistence covering Load Survey/Daily Load Profile
ingestion with VEE (negative-consumption, out-of-range, and missing-interval estimation, all with
an audit trail), meter master data with temporal installation/replacement history, a `config`
module (tariff categories, the electrical hierarchy, the organizational hierarchy), user/role
records (data only, no auth/enforcement yet), `energy-audit` (transparent supply-vs-consumption
balance, no automated real-loss/coverage-loss split), `complaints` (SLA-tracked tickets), meter
inventory allocation and a three-level installation quality check, `revenue-protection` (risk
signals and an investigation lead workflow), a `prepaid` wallet (ledger-based, idempotent recharge
and daily billing, connect/disconnect), tenant scoping on every table, EF Core migrations, and 111
passing backend unit tests — plus a working **frontend** (`frontend/`, Next.js + MUI + TanStack
Query) with a page for every module except meter inventory/installation QC. No authentication
exists on either side yet. This is a broad-but-shallow vertical slice across most of the spec's
modules, not a production-hardened system — see "Not yet built" for what's simplified or missing.
## Structure

- `backend/` — .NET 8 solution (`MDMS.sln`) — see note on target framework below
  - `MDMS.Api` — ASP.NET Core Web API (controllers, Swagger, migration bootstrap)
  - `MDMS.Application` — use cases (`LoadSurveyIngestionService`, `OutOfRangeValidationService`, ...), persistence port (`IMdmsDbContext`)
  - `MDMS.Domain` — entities, enums, no external dependencies beyond EF Core's `DbSet<T>` shape
    referenced from `IMdmsDbContext`
  - `MDMS.Infrastructure` — EF Core (PostgreSQL/Npgsql) persistence, entity configurations, migrations
  - `MDMS.Tests` — xUnit tests (EF Core InMemory provider)

Modules are organized as folders/namespaces within these four projects rather than separate
projects or services — a modular monolith. `MeterData` (ingestion + VEE), `Config` (reference
data), `EnergyAudit`, `Complaints`, meter inventory/installation-QC, and `RevenueProtection` are
the modules so far; `billing` and `wfm` are planned the same way (see "Not yet built").

**Target framework note**: the target stack is .NET 10, but only the .NET 8 SDK is available in
this environment (.NET 10 is still pre-GA as of writing). Everything here targets **net8.0** with
EF Core 8.x pinned explicitly (the SDK defaults to EF Core 10.x, which is net10.0-only). Bumping
every `<TargetFramework>` to `net10.0` and the EF Core/Npgsql package versions to their 10.x
releases is a mechanical change once that SDK is installed — no architectural changes needed.

## Tenant scoping

Every entity carries a `TenantId` (see `Entity.TenantId`), defaulting to a single well-known
`DefaultTenantId` until a real multi-tenant resolver exists. This is deliberately non-invasive:
no existing constructor had to change to accept a tenant parameter, since retrofitting a tenant
column later (once real per-request tenant context exists) is far cheaper than retrofitting it
across a live schema. `Entity.AssignTenant(...)` exists for that future resolver to call; nothing
calls it yet. A `Tenant` entity/table anchors the concept (one row today).

## Domain model (this slice)

| Entity | Purpose |
|---|---|
| `Tenant` | A DISCOM — the tenant boundary every other table scopes to |
| `Customer` | Utility account — thin master record; `AccountNumber` is the key a downstream billing system would reference this by |
| `ServicePoint` | A customer's physical point of supply — the stable identity a meter is installed against over time |
| `Meter` | Physical meter asset master data (serial number, phase, lifecycle status) |
| `MeterAssignment` | Temporal audit record binding a meter to a service point — installation, replacement (with closing/opening readings), removal. Guarantees an old meter's cumulative reading is never compared against a new meter's |
| `LoadSurveyInterval` | One 30-minute LS block, with explicit `Source` (Received/Estimated/Edited/Calculated) and `Quality` (Valid/NegativeConsumption/OutOfRange/...) |
| `DailyLoadProfile` | The meter's daily profile at the 00:00 boundary, with a provisional/estimated path for a missing day |
| `DataQualityHold` | An active hold blocking further processing for a meter after a data-quality event (e.g. negative consumption) — about measurement validity, not billing |
| `VeeRuleDefinition` | Abstract base for a stored, configurable VEE rule (table-per-hierarchy) — data, not a hard-coded constant. `MeasurementRangeThreshold` is the first concrete rule type |
| `MeasurementRangeThreshold` | A `VeeRuleDefinition` subclass: a configurable min/max plausibility range for LS or DLP consumption (`MeasurementType` keeps the two separate — their plausible scales are entirely different) — global default (`MeterId == null`) or meter-specific override, per measurement type |
| `TariffCategory` | A billing-category reference (Domestic, Industrial, ...) used to classify a customer for downstream TOD-slab mapping — classification only, no rate/slab math |
| `HierarchyNode` | One node of the **electrical** hierarchy — Substation → Feeder → Distribution Transformer — that energy-audit aggregation will roll up through. A single self-referencing tree, level enforced by `NodeType`. A `ServicePoint` links to a DT node to complete the chain to the consumer |
| `OrgUnit` | One node of the **organizational** hierarchy — Zone → Circle → Division → Sub Division → Section — used for RBAC scoping, dashboards, and work assignment. Deliberately a separate tree from `HierarchyNode`; the two never share levels or nodes |
| `User` | A named user with a fixed `Role` (12 roles: Admin, ItManager, Nomc, Supervisor, QualityIncharge, OmSupervisor, Installer, OmExecutive, Contractor, UtilityManager, ComplaintDesk, StoreManager) and an `OrgUnitId` scope (required for every role except Admin). **Data only** — no login/credentials/permission enforcement exists yet |
| `VeeExecutionRecord` | An immutable audit entry for one VEE rule execution against one measurement slot — rule name, outcome, estimated value if any, and a human-readable explanation. VEE's own audit trail |
| `NetworkEnergyReading` | The supply-side energy that entered one `HierarchyNode` (Substation/Feeder/DT) on one day — entered directly today, since there's no feeder/DTR meter ingestion pipeline yet |
| `Complaint` | A consumer complaint tracked Open → Assigned → InProgress → Resolved → Closed, with an SLA due-time from a caller-supplied duration. Closing requires having gone through Resolved first ("NOMC validation" per the spec) |
| `MeterInventoryRecord` | A meter's WFM/inventory allocation state machine (Received → InStore → AssignedToContractor → AssignedToInstaller → Installed → Commissioned → Active, plus a replacement sub-flow) — deliberately separate from `Meter.Status`, which is just the meter's own simple master-data lifecycle |
| `RevenueProtectionLead` | An investigation lead (Detected → Scored → Reviewed → Assigned → FieldInvestigation → FindingRecorded → ActionTaken → optionally RecoveryRecorded → Closed) — never a legal conclusion, per the spec's own caution |
| `RiskSignal` | One observed risk indicator contributing to a lead's `RiskScore` — the weight is caller-supplied, never a hard-coded table, since "the actual weights must be calibrated by the utility" |
| `PrepaidAccount` | A customer's prepaid wallet — balance is a denormalized read of its own ledger, never a bare mutable number |
| `WalletTransaction` | One immutable, append-only ledger entry (Recharge / ConsumptionDebit / Adjustment), carrying the balance it produced and an idempotency `Reference` |
| `InstallationQualityCheck` | The three-level installation quality check (Contractor L1 → Quality Incharge L2 → Utility Manager L3 → RMS sync → MIS onboarding). A rejection at L2 returns to L1Pending, at L3 to L2Pending — never all the way back to Draft |

### Load Survey ingestion

`LoadSurveyIngestionService.IngestAsync` validates a batch of LS blocks:

- Tracks each meter's last-known valid cumulative reading **across the whole in-flight batch**,
  not only already-persisted rows, so two blocks for the same meter in one request can't both
  incorrectly come back Valid when the second is actually a negative-consumption event. Covered by
  `IngestAsync_TwoBlocksInOneBatch_SecondNegative_IsDetectedWithinBatch`.
- A cumulative reading lower than the prior interval for the same meter is rejected
  (`NegativeConsumption`) and raises (or reactivates) a `DataQualityHold` for that meter — never
  silently zeroed.
- Idempotent: a duplicate interval (same `MeterId + IntervalStartUtc + IntervalEndUtc`), whether
  already persisted or repeated within the same batch, is a no-op, not an error. The database
  also enforces this uniquely as a second layer.
- A bad item never fails the rest of the batch.
- Also runs the out-of-range plausibility check inline (see below) against each interval's
  computed consumption before it's stored — an implausible reading is flagged the moment it
  arrives, not only on the next on-demand sweep.

### Daily Load Profile ingestion

`DailyLoadProfileIngestionService.IngestAsync`:

- No existing profile for that `ServicePointId + MeterId + ProfileDate` → stores it as `Received`.
- An existing **provisional** (`Estimated`/`Missing`) profile for that key → replaced by the real
  `Received` one; the provisional row is not left behind.
- An existing **`Received`** profile for that key → rejected (`409 Conflict`) rather than
  overwritten — a genuine received value is never silently replaced.
- Also runs the out-of-range plausibility check inline (see below), same as LS ingestion.

### Out-of-range VEE checks

`OutOfRangeValidationService` is the second VEE check, alongside inline negative-consumption
detection in LS ingestion — a configurable plausibility range, applied to **both** Load Survey
and Daily Load Profile consumption via `MeasurementRangeType` (kept strictly separate: a 30-minute
LS interval's plausible range and a full day's DLP range are entirely different scales, and a
threshold configured for one type is never consulted for the other).

- A meter-specific `MeasurementRangeThreshold` takes precedence over the global default for that
  type (`MeterId == null`); a meter with neither configured — or only an inactive one — is never
  flagged for that type. No threshold means no opinion, not a false positive.
- **Runs inline during ingestion**: both `LoadSurveyIngestionService` and
  `DailyLoadProfileIngestionService` resolve the effective threshold for their respective type and
  flag a breach `OutOfRange` via `FlagOutOfRange()` before the row is ever stored as `Valid` — the
  consumption value itself is never altered, only the quality annotation. For LS specifically, an
  out-of-range interval is a plausibility flag, not a sequence break: its `CumulativeReading` is
  still trusted as the anchor for the next interval's delta, and it never raises a
  `DataQualityHold` on its own (unlike negative consumption). The "last known reading"
  sequence-continuity query correspondingly treats `OutOfRange` as trustworthy and only excludes
  `NegativeConsumption` rows.
- **Also available on demand**: `POST /api/v1/vee/out-of-range-checks/ls/run` and
  `.../dlp/run` re-evaluate every currently-`Valid` row of that type for a meter (or every meter
  with a configured threshold of that type, if none is specified) — useful for retroactively
  applying a threshold added/corrected *after* data was already ingested, which the inline check
  alone can't do.
- **`MeasurementRangeThreshold` is a `VeeRuleDefinition`**, not a standalone table: rules of every
  type share one `VeeRuleDefinitions` table (discriminated by `RuleType`) with common fields
  (`MeterId`, `IsActive`) on the shared base and type-specific fields (here, `Min`/`MaxConsumptionKwh`)
  on the subclass — so a future config screen can list every rule of every type in one query, and
  a future rule type (continuity, missing-interval estimation) is a new subclass, not a rewrite of
  this one. `IsActive` is enforced: a deactivated threshold is treated exactly like no threshold.

### Missing-interval estimation

`MissingIntervalEstimationService` covers the completeness/estimation half of VEE: a day has 48
expected 30-minute LS slots (`ExpectedSlots`), and `DetectMissingSlotsAsync` finds any with no
`LoadSurveyInterval` row at all. `EstimateMissingSlotsAsync` estimates a gap only when both
immediate neighbors (the interval ending exactly at the slot's start, and the one starting exactly
at its end) exist and are `Valid`, using the "average of surrounding periods" method — one of the
estimation approaches this project's spec allows. A slot without both usable neighbors is never
guessed at or treated as zero consumption; it stays missing. **Every attempt is recorded** as a
`VeeExecutionRecord` (rule name, slot, outcome, estimated value if any, and a human-readable
explanation) — VEE's own audit trail, independent of and in addition to `DataQualityHold`.
`GET /api/v1/vee/estimation/ls/missing-slots` and `POST /api/v1/vee/estimation/ls/run` expose this;
`GET /api/v1/vee/execution-records` exposes the audit trail itself.

### Energy audit

`EnergyAuditService` compares energy entering a network level against energy accounted for
downstream — deliberately **not** collapsed into a single "loss %". `ComputeDailyBalanceAsync`
returns `EnergyBalanceResult`: the supply-side reading (if any), the summed downstream Daily Load
Profile consumption for that day, the discrepancy, and — separately — data completeness (what
fraction of linked service points actually reported a profile at all that day). The idea is that a
reader can judge how much of a discrepancy is a genuine loss versus a data-coverage gap, rather
than trusting an unjustified automated real-loss/coverage-loss split formula.

- `GetDescendantDistributionTransformerIdsAsync` walks the `HierarchyNode` tree from any node down
  to its DT descendants (or returns itself if already a DT) — a consumer only ever attaches at the
  DT level via `ServicePoint.DistributionTransformerNodeId`, so this determines exactly which
  consumers roll up under a Substation, Feeder, or DT balance calculation.
- No feeder/DTR meter ingestion pipeline exists yet, so `NetworkEnergyReading` (the supply-side
  figure) is entered directly via `POST /api/v1/energy-audit/network-energy-readings` rather than
  derived from a real boundary meter feed.
- `GET /api/v1/energy-audit/balance?hierarchyNodeId=&date=` exposes the full transparent
  calculation.

### Config module

`ConfigController` (`/api/v1/config`) covers reference/hierarchy master data that other modules
will depend on:

- **Tariff categories** (`/tariff-categories`): a simple code/name/description reference used to
  classify customers for downstream billing categorization — MDMS does not compute tariff rates
  or slabs itself.
- **Electrical hierarchy** (`/hierarchy`): Substation/Feeder/Distribution-Transformer nodes as one
  self-referencing tree, for energy-audit aggregation. `HierarchyNode.CreateChild` enforces that a
  node only ever nests directly under a parent exactly one level coarser (Feeder under Substation,
  DT under Feeder) — never skipping a level.
- **Organizational hierarchy** (`/org-units`): Zone/Circle/Division/Sub-Division/Section nodes as
  a *separate* self-referencing tree, for RBAC scoping and dashboards — never conflated with the
  electrical hierarchy above, since they serve different purposes and don't share levels.
  `OrgUnit.CreateChild` enforces the same never-skip-a-level rule across all five levels.
- **Users** (`/api/v1/users`, `UsersController`): `Username`/`DisplayName`/`Role`/`OrgUnitId`
  records. Every role except Admin must be scoped to an `OrgUnit` — enforced in the `User`
  constructor. This is identity/authorization *data* only; there is no login, credential, or
  permission-enforcement mechanism yet.
- No role/authorization check is applied to these endpoints yet — that needs a decision on an auth
  package (JWT bearer vs. another scheme) before it can be added; see "Not yet built".

### Complaints

`Complaint` (`ComplaintsController`, `/api/v1/complaints`) tracks a consumer ticket through
Open → Assigned → InProgress → Resolved → Closed:

- The SLA due-time is computed from a caller-supplied duration at creation — never a hard-coded
  number of hours in code, per the project's "configuration over hard-coded rules" principle.
  `IsOverdue(nowUtc)` is `true` whenever the current time is past that due-time and the complaint
  isn't `Closed`.
- **Closing requires having gone through `Resolved` first** — a field action can never self-close
  a ticket; the spec's "NOMC validation" step is enforced as a separate required transition, not a
  convention callers have to remember.
- Every transition (`AssignTo`, `StartProgress`, `Resolve`, `Close`) throws
  `InvalidOperationException` from the wrong starting status (surfaced as `409 Conflict` by the
  API) — e.g. `Close()` throws unless the complaint is currently `Resolved`.
- Not yet built: the spec's automated ticket triggers (non-communicating/never-communicating
  meter, missing DP/BP, reconnection-SLA-breach) — every `Complaint` today is raised through the
  API, never auto-generated by a background process.

### Meter inventory & installation quality check

`MeterInventoryController` (`/api/v1/meter-inventory`) covers two separate state machines per
meter, matching the spec's own separation of "is this meter allocated/installed" from "was that
installation quality-approved":

- **`MeterInventoryRecord`**: Received → InStore → AssignedToContractor → AssignedToInstaller →
  Installed → Commissioned → Active, plus a replacement sub-flow (Active →
  ReplacementRequested → Removed, requiring a reason). Deliberately separate from `Meter.Status`
  — that's the meter's own simple master-data lifecycle, not this richer allocation workflow
  involving contractor/installer actors.
- **`InstallationQualityCheck`**: the spec's three-level check — Draft → L1Pending → L1Completed
  (Contractor) → L2Pending → L2Approved (Quality Incharge) → L3Pending → L3Approved (Utility
  Manager) → RmsSyncPending → RmsSynced → MisOnboarded. A rejection at L2 returns to `L1Pending`
  (redo the field work); a rejection at L3 returns to `L2Pending` (redo the L2 review) — never all
  the way back to `Draft`, and every rejection requires a note.
- Every wrong-state transition on either entity throws `InvalidOperationException` (surfaced as
  `409 Conflict`), matching the rest of the codebase's transition discipline.
- **No frontend page yet** for either workflow — backend-only so far, unlike every other module.

### Revenue protection

`RevenueProtectionController` (`/api/v1/revenue-protection`) implements the spec's lead-generation
workflow — deliberately framed as investigation support, never a legal conclusion:

- `RevenueProtectionService.RaiseSignalAsync` finds the customer's current open (non-`Closed`)
  lead, or creates one, then adds a `RiskSignal` and its weight to that lead's `RiskScore`. One
  open lead per customer at a time — a second signal contributes to the existing investigation
  rather than fragmenting it.
- **Signal weights are caller-supplied, never a hard-coded table in this project** — per the
  spec's own caution that weights "must be calibrated by the utility and should not be presented
  as legal proof."
- Lead lifecycle: Detected → Scored (on the first signal) → Reviewed → Assigned →
  FieldInvestigation → FindingRecorded → ActionTaken → Closed. `RecordRecovery` (→
  `RecoveryRecorded`) is optional between ActionTaken and Closed — a field visit that turns out to
  be a false positive still needs a documented closure, not a forced recovery amount.

### Prepaid

`PrepaidController` (`/api/v1/prepaid`) implements a ledger-based prepaid wallet:

- **Ledger-based, never a bare mutable balance** — `PrepaidAccount.Balance` only ever changes
  through a method (`Recharge`, `DebitForConsumption`, `Adjust`) that also returns the
  `WalletTransaction` recording it, carrying the resulting balance.
- **Both mutating operations are idempotent** on a caller-supplied `Reference`:
  `PrepaidService.RechargeAsync` returns the original transaction on a repeated reference (a
  duplicated payment-gateway callback never double-credits), and `ProcessDailyBillingAsync` uses a
  reference derived from `(customerId, date)` so re-running a day's billing is a safe no-op.
  `WalletTransaction.Reference` also carries a DB-enforced unique constraint as a second layer.
- Daily billing sums that day's Daily Load Profile consumption across every service point linked
  to the customer and charges it at a **caller-supplied flat rate per kWh** — a deliberate
  placeholder, not a real tariff engine (no slabs, categories, or ToD). A day with zero
  consumption data returns `null`/`204` rather than fabricating a zero-consumption charge.
  `DebitForConsumption` never rejects on insufficient balance — going negative/overdraft is a
  utility policy decision, not this entity's to enforce.
- **No HES/meter-command integration** — `Disconnect`/`Reconnect` only flip the account's own
  status flag; there is no dispatched-and-acknowledged physical meter command here.
  `Reconnect()` still requires a positive balance before it succeeds.

## Getting started

```bash
cd backend
dotnet build MDMS.sln
```

### Database setup

1. Create the database: `createdb mdms` (or `psql`: `CREATE DATABASE mdms;`).
2. Set the real connection string via .NET User Secrets (do not commit real credentials —
   `appsettings.json` only holds a placeholder password):
   ```bash
   cd backend/MDMS.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:Mdms" "Host=localhost;Port=5432;Database=mdms;Username=postgres;Password=<your-password>"
   ```
3. Apply the migration:
   ```bash
   cd backend
   dotnet tool restore
   dotnet tool run dotnet-ef database update \
     --project MDMS.Infrastructure/MDMS.Infrastructure.csproj \
     --startup-project MDMS.Api/MDMS.Api.csproj
   ```
4. Run: `dotnet run --project MDMS.Api`, then `curl http://localhost:<port>/health` →
   `{"status":"Healthy"}`, Swagger UI at `http://localhost:<port>/swagger`.

In Development, the app auto-applies pending migrations on startup (no seeding yet).

Add a new migration after changing the model:
```bash
dotnet tool run dotnet-ef migrations add <Name> \
  --project MDMS.Infrastructure/MDMS.Infrastructure.csproj \
  --startup-project MDMS.Api/MDMS.Api.csproj \
  --output-dir Persistence/Migrations
```

### API endpoints

| Endpoint | Purpose |
|---|---|
| `GET /health` | Liveness check |
| `GET /api/v1/meters` / `GET /api/v1/meters/{id}` | Meter master data |
| `POST /api/v1/meters` | Register a new meter |
| `POST /api/v1/meters/{id}/install` | Record initial installation at a service point |
| `POST /api/v1/meters/{id}/replace` | Record a meter replacement with closing/opening readings |
| `GET /api/v1/meters/replacements` | Full meter replacement history, cross-meter |
| `POST /api/v1/meter-data/ls` | Ingest a batch of 30-minute LS blocks |
| `GET /api/v1/meter-data/ls?meterId=` | Recent LS intervals (capped at 500), optionally filtered by meter |
| `POST /api/v1/meter-data/dlp` | Ingest one Daily Load Profile (replaces a provisional profile if one exists; `409` if a received one already exists for that meter/date) |
| `GET /api/v1/meter-data/dlp?meterId=` | Recent Daily Load Profiles (capped at 500), optionally filtered by meter |
| `GET /api/v1/meter-data/billing-holds?activeOnly=` | Data-quality holds |
| `POST /api/v1/meter-data/{meterId}/billing-hold/clear` | Clear an active hold with a mandatory resolution note |
| `GET /api/v1/vee/thresholds?measurementType=` | List configured out-of-range thresholds (global default first per type); omit `measurementType` for all |
| `POST /api/v1/vee/thresholds` | Configure a plausibility threshold for a `measurementType` (`LoadSurveyInterval` or `DailyLoadProfile`) — global (`meterId: null`) or meter-specific |
| `POST /api/v1/vee/out-of-range-checks/ls/run?meterId=` | Re-evaluate stored LS intervals against their effective threshold, flagging breaches `OutOfRange`; omit `meterId` to sweep every meter with a configured LS threshold |
| `POST /api/v1/vee/out-of-range-checks/dlp/run?meterId=` | Same, for Daily Load Profiles |
| `GET /api/v1/vee/estimation/ls/missing-slots?meterId=&date=` | List the day's missing 30-min LS slots for a meter (detection only) |
| `POST /api/v1/vee/estimation/ls/run?meterId=&date=` | Estimate missing LS slots via average-of-neighbors where possible; returns the audit trail |
| `GET /api/v1/vee/execution-records?meterId=` | The VEE audit trail (every rule execution recorded) |
| `GET /api/v1/config/tariff-categories` / `POST /api/v1/config/tariff-categories` | List / create tariff category reference data |
| `GET /api/v1/config/hierarchy?nodeType=` | List electrical hierarchy nodes (Substation/Feeder/DT), optionally filtered by level |
| `GET /api/v1/config/hierarchy/{id}/children` | List a node's direct children |
| `POST /api/v1/config/hierarchy` | Create an electrical hierarchy node (Substation needs no parent; Feeder/DT each require a parent exactly one level coarser) |
| `GET /api/v1/config/org-units?unitType=` | List organizational hierarchy units (Zone/Circle/Division/Sub Division/Section), optionally filtered by level |
| `GET /api/v1/config/org-units/{id}/children` | List a unit's direct children |
| `POST /api/v1/config/org-units` | Create an org unit (Zone needs no parent; every other level requires one exactly one level coarser) |
| `GET /api/v1/users?role=` / `GET /api/v1/users/{id}` | List / get user records, optionally filtered by role |
| `POST /api/v1/users` | Create a user (`role` + `orgUnitId`; every role except Admin requires an org unit) |
| `POST /api/v1/users/{id}/reassign` | Change a user's role and/or org unit |
| `POST /api/v1/energy-audit/network-energy-readings` | Record (or revise) a hierarchy node's supply-side energy for one day |
| `GET /api/v1/energy-audit/network-energy-readings?hierarchyNodeId=` | List recorded network energy readings |
| `GET /api/v1/energy-audit/balance?hierarchyNodeId=&date=` | The transparent energy balance for a node/day (input, accounted, discrepancy, data completeness) |
| `GET /api/v1/complaints?status=&overdueOnly=` | List complaints, optionally filtered by status or SLA-overdue |
| `GET /api/v1/complaints/{id}` | Full complaint detail |
| `POST /api/v1/complaints` | Raise a complaint (`customerId`, optional `meterId`, `source`, `description`, `slaHours`) |
| `POST /api/v1/complaints/{id}/assign` | Assign to a user — `409` if already Resolved/Closed |
| `POST /api/v1/complaints/{id}/start-progress` | Move Assigned → InProgress — `409` otherwise |
| `POST /api/v1/complaints/{id}/resolve` | Move to Resolved with a mandatory resolution note |
| `POST /api/v1/complaints/{id}/close` | Move Resolved → Closed — `409` unless already Resolved |
| `POST /api/v1/meter-inventory/{meterId}/receive` | Start a meter's inventory record at `Received` |
| `GET /api/v1/meter-inventory/{meterId}` / `GET /api/v1/meter-inventory?status=` | Get / list inventory records |
| `POST /api/v1/meter-inventory/{meterId}/{move-to-store,assign-contractor,assign-installer,mark-installed,commission,activate,request-replacement,mark-removed}` | Inventory transitions, in order — `409` from the wrong status |
| `POST /api/v1/meter-inventory/{meterId}/quality-check` | Start a three-level installation quality check at `Draft` |
| `GET /api/v1/meter-inventory/quality-checks/{id}` / `GET .../quality-checks?status=` | Get / list quality checks |
| `POST /api/v1/meter-inventory/quality-checks/{id}/{start-l1,complete-l1,submit-l2,approve-l2,reject-l2,submit-l3,approve-l3,reject-l3,rms-sync-pending,rms-synced,mis-onboarded}` | Quality-check transitions — `409` from the wrong status; the two `reject-*` endpoints require a note |
| `POST /api/v1/revenue-protection/signals` | Raise a risk signal (creates/reuses an open lead for the customer) |
| `GET /api/v1/revenue-protection/leads?status=` | List leads, highest risk first |
| `GET /api/v1/revenue-protection/leads/{id}` | Full lead detail plus its raised signals |
| `POST /api/v1/revenue-protection/leads/{id}/{review,assign,start-investigation,record-finding,record-action,record-recovery,close}` | Lead transitions, in order — `409` from the wrong status |
| `GET /api/v1/prepaid/accounts/{customerId}` | Get a prepaid account |
| `GET /api/v1/prepaid/accounts/{customerId}/transactions` | The account's ledger (most recent 500) |
| `POST /api/v1/prepaid/recharge` | Idempotent recharge on `reference` — opens the account if it doesn't exist yet |
| `POST /api/v1/prepaid/daily-billing` | Idempotent per `(customerId, date)`; `204` if there's no consumption data for that day |
| `POST /api/v1/prepaid/accounts/{customerId}/disconnect` | Flip the account to disconnected — `409` if already disconnected |
| `POST /api/v1/prepaid/accounts/{customerId}/reconnect` | Flip to connected — `409` if already connected or balance isn't positive |
| `GET /swagger` | Interactive API docs (Development only) |

**No authentication is wired up yet** — this is a local-development skeleton, not intended for
shared/production exposure. Add auth before that changes.

### Testing

```bash
cd backend
dotnet test MDMS.sln
```

**111 tests, all passing** — covering LS ingestion's happy path, consumption-delta calculation,
negative-consumption rejection + hold creation, the in-batch negative-consumption detection
regression, idempotency (duplicate-in-batch and already-persisted duplicate), inline out-of-range
flagging during LS ingestion (including that a flagged interval still anchors the next interval's
sequence and never raises a hold), DLP ingestion (new profile, provisional replacement, rejecting
an overwrite of a received profile, inline out-of-range flagging), threshold resolution
(meter-specific vs. global vs. unconfigured, and that LS/DLP thresholds are looked up
independently and never leak into each other), the on-demand out-of-range check for both LS and
DLP, the `VeeRuleDefinition`/`MeasurementRangeThreshold` table-per-hierarchy round-trip (including
that a deactivated rule is no longer the effective one), tenant-default assignment, tariff
category validation, electrical-hierarchy-level enforcement, organizational-hierarchy-level
enforcement, `ServicePoint`-to-DT-node linking, `User` role/org-scope validation (every non-Admin
role requires an org unit), missing-slot detection, missing-interval estimation (both the
successful average-of-neighbors case and the "never guess without both neighbors" case), hierarchy
descendant-DT resolution from any level, energy-balance calculation (discrepancy math, data
completeness, and the no-reading-yet case), the complaint lifecycle (every legal/illegal
transition, SLA overdue math), the full meter-inventory happy path plus its replacement sub-flow,
the installation quality check (full happy path to MisOnboarded, both rejection paths landing
at the correct level, and rejecting a blank note), and revenue-protection (score accumulation
across multiple signals, the full lead lifecycle with and without a recorded recovery, that a
second signal for the same customer joins the existing open lead rather than fragmenting it, and
that a signal for a customer with only a Closed lead starts a fresh one), and the prepaid wallet
(recharge/debit balance math, recharge idempotency on a repeated reference, daily-billing
idempotency per customer/date, the no-consumption-data no-op, and the reconnect-requires-positive-
balance rule).

## Frontend

See [`frontend/README.md`](frontend/README.md) — Next.js + MUI + TanStack Query, one page per
backend module (Meters, Meter Data, VEE, Config, Users, Energy Audit, Complaints, Revenue Protection, Prepaid). No auth, no
charts yet, and no screens for modules that don't exist on the backend (revenue protection,
prepaid, WFM, billing).

## Not yet built

- A fuller VEE rules engine beyond negative-consumption, out-of-range, and average-of-neighbors
  missing-interval estimation (explicit timestamp/boundary validation, DLP-side missing-day
  estimation, provisional-DLP auto-creation, duplicate-message detection).
- A real feeder/DTR meter ingestion pipeline for `NetworkEnergyReading` (entered manually today)
  and an automated, justified real-loss vs. coverage-loss split (today's `energy-audit` module
  reports the raw discrepancy and data-completeness transparently, but deliberately does not
  attempt that split without a defensible formula).
- Automated revenue-protection signal detection — today every `RiskSignal` is raised through the
  API by an operator/external process; nothing in MDMS itself analyzes consumption/tamper data
  and raises a signal on its own.
- A real tariff engine for prepaid daily billing — today's flat rate × kWh is a deliberate
  placeholder; slabs, categories, and ToD mapping (using `TariffCategory`) don't feed into a charge
  calculation yet, for prepaid or for a `billing` (postpaid bill determinant) module, which doesn't
  exist at all.
- A dispatched-and-acknowledged disconnect/reconnect command workflow — today's prepaid
  `Disconnect`/`Reconnect` just flip a status flag; there's no HES/meter-command integration to
  confirm a physical meter actually changed state (the spec's "sent isn't the same as completed"
  principle isn't enforceable yet without that integration).
- A `wfm` module proper (work-order assignment tying an installation/replacement to a contractor
  and installer end-to-end) — meter inventory and installation QC (see above) exist as their own
  state machines but nothing yet connects them into a work-order object. `Complaints` exists (see
  above) but its automated-ticket triggers (non-communicating meter, missing DP/BP,
  reconnection-SLA-breach) don't — tickets are only raised through the API today.
- Frontend pages for meter inventory and installation QC — both are backend-only so far.
- Authentication/authorization enforcement — the `User`/`Role`/`OrgUnit` *data model* now exists,
  but nothing checks it yet; needs an auth package decision (JWT bearer vs. another scheme) first.
- IP (15-minute Instantaneous Profile) and BP (monthly Billing Profile) ingestion — only LS
  (30-min) and DP/DLP (daily) exist today.
- A real multi-tenant resolver — `TenantId` exists on every table today but always defaults to one
  well-known tenant; nothing yet assigns a different one per request.
- Seed data / demo data.
