# MDMS — Full Project Detail

_Generated as a status/reference document. Reflects the state as of this session, including work
still uncommitted on the local branch `feature/config-module` (explicitly called out below)._

---

## 1. What this project is

A **Meter Data Management System (MDMS)** for electricity utilities: it owns meter master data,
ingests and validates smart-meter measurement data (Load Survey intervals, Daily Load Profiles),
and — as of this session — reference/hierarchy configuration (tariff categories, DISCOM
organizational hierarchy, VEE rule definitions).

**Explicit non-scope**: tariff rate calculation, wallets, billing, recharge, and RC/DC belong to a
separate downstream billing system. MDMS never duplicates that logic — where a link is needed
(e.g. a customer's account number), MDMS exposes the key for the other system to reference, not a
copy of the other system's data.

Repository: `https://github.com/lalit-prakash/MDMS`

---

## 2. Tech stack

| Layer | Choice | Note |
|---|---|---|
| Backend | C# / ASP.NET Core Web API | Target framework is **net8.0** — the intended net10.0 SDK isn't installed in this environment yet (pre-GA). Bumping the TFM + EF Core/Npgsql package versions once that SDK exists is a mechanical change, no architecture impact. |
| ORM | Entity Framework Core 8.x | Explicitly pinned (the `dotnet` CLI defaults to EF Core 10.x, which is net10.0-only) |
| Database | PostgreSQL (via Npgsql) | No live database has been provisioned/migrated against in this environment — schema is verified via `dotnet-ef migrations script`, not a live `database update` |
| Testing | xUnit + EF Core InMemory provider | 38 tests, all passing as of the last verified build |
| Frontend | **Not started** | Next.js/React/MUI/TanStack Query/ECharts is the intended stack; deliberately deferred to build the backend domain first |

Architecture style: a **modular monolith** — one ASP.NET Core solution, one deployable, with
modules kept as separate folders/namespaces (not separate projects or services) inside four
projects:

```
backend/
  MDMS.Domain/          — entities, enums; zero external dependencies
  MDMS.Application/     — use-case services, the IMdmsDbContext persistence port
  MDMS.Infrastructure/  — EF Core, Postgres, entity configurations, migrations
  MDMS.Api/             — controllers, Swagger, startup
  MDMS.Tests/           — xUnit tests (EF Core InMemory)
```

---

## 3. Domain model (everything that exists today)

### Master data

| Entity | Purpose |
|---|---|
| `Tenant` | A DISCOM — the tenant boundary every other table scopes to. One row exists conceptually today (`Entity.DefaultTenantId`). |
| `Customer` | A utility account. Thin record — `AccountNumber` is the key a downstream billing system would reference this by; MDMS never stores tariff/wallet/billing facts here. |
| `ServicePoint` | A customer's physical point of supply. The stable identity a meter is installed against over time (not the meter itself, since meters get replaced). |
| `Meter` | Physical meter asset (serial number, phase, lifecycle status: InStock / Installed / Removed / Retired). |
| `MeterAssignment` | Temporal audit record binding a meter to a service point: installation, replacement (with closing/opening cumulative readings), removal. The entire point of this record is that an old meter's cumulative reading is never compared against a new meter's. |

### Measurement / VEE

| Entity | Purpose |
|---|---|
| `LoadSurveyInterval` | One 30-minute Load Survey (LS) block for a meter. Carries `CumulativeReading`, computed `ConsumptionKwh`, explicit `Source` (Received / Estimated / Edited / Calculated), and `Quality` (Valid / NegativeConsumption / OutOfRange / Missing / Suspect). |
| `DailyLoadProfile` | The meter's authoritative daily profile at the 00:00 boundary. Same Source/Quality provenance discipline as LS; supports a "provisional" (estimated) state for a day whose real DLP hasn't arrived yet. |
| `DataQualityHold` | An active hold blocking further processing for one meter after a data-quality event (e.g. negative consumption). About measurement validity, not billing — a downstream consumer should treat a held meter as having no trustworthy new data. |
| `VeeRuleDefinition` | **Abstract base** for any stored, configurable VEE rule. Table-per-hierarchy: one `VeeRuleDefinitions` table, discriminated by `RuleType`, so a future config UI can list every rule of every type in one query. Carries `MeterId` (null = global default) and `IsActive`. |
| `MeasurementRangeThreshold` | The first concrete `VeeRuleDefinition` subclass — a configurable min/max plausibility range, scoped by `MeasurementRangeType` (LoadSurveyInterval vs. DailyLoadProfile, since their plausible scales are entirely different and a threshold for one is never consulted for the other). |

### Config / reference data (new this session)

| Entity | Purpose |
|---|---|
| `TariffCategory` | A billing-category reference (Domestic, Industrial, ...) — classification only. Used later for TOD-slab mapping; MDMS does not compute tariff rates itself. |
| `HierarchyNode` | One node of the Circle → Division → Feeder → Distribution Transformer hierarchy, as a single self-referencing tree (`NodeType` enum + `ParentId`). `HierarchyNode.CreateChild(...)` enforces that a node only nests directly under a parent exactly one level coarser — never skipping a level. |

### Cross-cutting: tenant scoping

Every entity inherits `TenantId` from the base `Entity` class, defaulting to a well-known
`Entity.DefaultTenantId` value. This was done **non-invasively** — no existing constructor needed
to change to accept a tenant parameter. `Entity.AssignTenant(Guid)` exists for a future
multi-tenant resolver (part of an eventual `iam` module) to call; nothing calls it yet.

---

## 4. Application-layer services (the actual business logic)

### `LoadSurveyIngestionService`

Validates and stores a batch of LS interval readings:
- Tracks each meter's last-known valid cumulative reading **across the entire in-flight batch**,
  not just already-persisted rows — this specifically prevents two blocks for the same meter in
  one request from both incorrectly validating when the second is actually a negative-consumption
  event.
- A cumulative reading lower than the prior one for the same meter → rejected as
  `NegativeConsumption`, raises (or reactivates) a `DataQualityHold`.
- Duplicate detection (same meter + interval window), both within-batch and against already-saved
  rows — idempotent, not an error.
- A bad item never fails the rest of the batch.
- Runs the out-of-range check inline (see below) before persisting.

### `DailyLoadProfileIngestionService`

- No existing profile for a given `ServicePointId + MeterId + ProfileDate` → stored as `Received`.
- An existing **provisional** profile for that key → replaced by the real one.
- An existing **Received** profile for that key → rejected with `409 Conflict` — never silently
  overwritten.
- Also runs the out-of-range check inline.

### `OutOfRangeValidationService`

The second VEE check (alongside inline negative-consumption detection):
- `GetEffectiveThresholdAsync(meterId, type)` — resolves a meter-specific threshold if one exists
  and is active, else the global default for that type if active, else `null` (no threshold means
  no opinion, not a false positive).
- `RunLoadSurveyCheckAsync` / `RunDailyLoadProfileCheckAsync` — on-demand re-evaluation of already
  stored `Valid` rows, useful for retroactively applying a threshold added *after* ingestion.
- A flagged `OutOfRange` row never has its actual value mutated — only the quality annotation
  changes. For LS specifically, an out-of-range interval still anchors the next interval's
  sequence-continuity check (it's a plausibility flag, not a sequence break) and never raises a
  `DataQualityHold` on its own.

### `ConfigController` (API-only so far, no dedicated Application service yet)

CRUD for `TariffCategory` and `HierarchyNode`, directly against `IMdmsDbContext`. No
authentication/authorization is applied — an explicit, documented gap pending an auth package
decision.

---

## 5. API surface (all endpoints that exist)

| Endpoint | Purpose |
|---|---|
| `GET /health` | Liveness check |
| `GET /api/v1/meters`, `GET /api/v1/meters/{id}` | Meter master data |
| `POST /api/v1/meters` | Register a meter |
| `POST /api/v1/meters/{id}/install` | Record initial installation |
| `POST /api/v1/meters/{id}/replace` | Record a replacement (with closing/opening readings) |
| `GET /api/v1/meters/replacements` | Full replacement history |
| `POST /api/v1/meter-data/ls` | Ingest a batch of LS blocks |
| `GET /api/v1/meter-data/ls?meterId=` | Recent LS intervals (capped at 500) |
| `POST /api/v1/meter-data/dlp` | Ingest one DLP |
| `GET /api/v1/meter-data/dlp?meterId=` | Recent DLPs (capped at 500) |
| `GET /api/v1/meter-data/billing-holds?activeOnly=` | List data-quality holds |
| `POST /api/v1/meter-data/{meterId}/billing-hold/clear` | Clear a hold (mandatory resolution note) |
| `GET /api/v1/vee/thresholds?measurementType=` | List out-of-range thresholds |
| `POST /api/v1/vee/thresholds` | Configure a threshold (global or meter-specific) |
| `POST /api/v1/vee/out-of-range-checks/ls/run?meterId=` | On-demand LS out-of-range sweep |
| `POST /api/v1/vee/out-of-range-checks/dlp/run?meterId=` | On-demand DLP out-of-range sweep |
| `GET /api/v1/config/tariff-categories`, `POST /api/v1/config/tariff-categories` | Tariff category reference data |
| `GET /api/v1/config/hierarchy?nodeType=` | List hierarchy nodes |
| `GET /api/v1/config/hierarchy/{id}/children` | List a node's children |
| `POST /api/v1/config/hierarchy` | Create a hierarchy node |
| `GET /swagger` | Interactive API docs (Development only) |

**No authentication exists on any endpoint.** This is a local-development skeleton, not intended
for shared or production exposure yet.

---

## 6. Testing

38 tests, all passing (as of the last verified build), covering:
- LS ingestion: happy path, consumption-delta math, negative-consumption rejection + hold
  creation, the in-batch negative-consumption detection regression, duplicate idempotency, inline
  out-of-range flagging (and that a flagged interval still anchors sequence continuity and never
  raises a hold).
- DLP ingestion: new profile, provisional replacement, reject-overwrite-of-received, inline
  out-of-range flagging.
- Threshold resolution: meter-specific vs. global vs. unconfigured vs. deactivated; LS and DLP
  thresholds never leak into each other.
- On-demand out-of-range checks for both LS and DLP.
- The `VeeRuleDefinition`/`MeasurementRangeThreshold` table-per-hierarchy round-trip through EF
  Core (confirms the generalization didn't change persisted behavior).
- Tenant-default assignment.
- `TariffCategory` validation.
- `HierarchyNode` level-nesting enforcement (correct nesting succeeds; wrong-level and
  Circle-as-child both throw).

---

## 7. Git / repository state — important, read this

- **`main`** is up to date and healthy: the LS/DLP ingestion + out-of-range VEE work (27 tests) is
  merged and verified.
- **This session's config-module work (TenantId, `VeeRuleDefinition` generalization,
  `TariffCategory`, `HierarchyNode`, `ConfigController`, plus the removal of every prepaid_engine
  reference from code/docs) is currently uncommitted**, sitting on local branch
  `feature/config-module`. It has **not** been committed, pushed, or opened as a PR — the user
  asked to hold all tasks before that happened. Build is clean and 38/38 tests pass locally.
- **A recurring process issue happened twice in earlier work**: a commit was pushed to a PR branch
  *after* that PR had already been merged, so it never reached `main` and had to be recovered via
  cherry-pick onto a new branch. Worth double-checking `main`'s content against the intended scope
  before assuming a PR merge captured everything.

---

## 8. Explicit design decisions worth knowing

1. **No microservices, no Kafka, no Docker, no separate deploy targets** — a plain modular
   monolith, per the project's own stated constraint to keep local development and debugging simple.
2. **Standalone from any other repository.** An earlier design assumed a companion billing engine
   repository (`prepaid_engine`) as a modeling reference; that reference has been fully removed
   from code comments and documentation this session, per instruction — MDMS is documented and
   designed as self-contained.
3. **Never overwrite raw/received measurements.** Every entity distinguishes `Received` from
   `Estimated`/`Edited`/`Calculated` explicitly (`MeasurementSource`), and quality outcomes
   (`MeasurementQuality`) are always an annotation, never a mutation of the underlying value.
4. **Configuration over hard-coded rules.** VEE thresholds are database rows
   (`VeeRuleDefinition`/`MeasurementRangeThreshold`), not constants in code.
5. **Temporal/audit-first for physical changes.** Meter replacement is a recorded event
   (`MeterAssignment`) with old/new readings, never an in-place field update.
6. **Tenant column added early, resolver deferred.** Every table has `TenantId` now; the
   *mechanism* to assign a non-default tenant per request doesn't exist yet (needs an `iam`
   module), and that gap is documented rather than faked.

---

## 9. What's explicitly not built yet

- Frontend (Next.js/React/MUI/TanStack Query/ECharts).
- A fuller VEE engine: continuity checks beyond negative-consumption, missing-interval
  estimation, automatic provisional-DLP creation.
- `energy-audit` module (DT/feeder/circle aggregation, AT&C loss, real-loss vs. coverage-loss
  split) — the `HierarchyNode` tree this would aggregate through now exists, but no aggregation
  logic does yet.
- `billing` module (bill determinant generation from validated interval data using
  `TariffCategory`).
- `wfm` and `complaints` modules.
- Authentication/authorization — blocked on choosing an auth package/scheme.
- A real multi-tenant resolver.
- Seed/demo data.
- The integration port to any downstream billing system (nothing on either side consumes MDMS's
  validated data yet).
