# MDMS

Meter Data Management System — meter master data, measurement ingestion/VEE, and load-profile
data, built as the upstream trusted-data source for downstream billing engines such as
[`prepaid_engine`](https://github.com/lalit-prakash/prepaid_engine).

**Scope**: this repository owns meter/service-point/customer master data, meter installation and
replacement history, Load Survey (LS) and Daily Load Profile (DLP) ingestion, and measurement
data-quality (VEE) processing. It does **not** own tariffs, wallets, billing, recharge, or RC/DC —
that is `prepaid_engine`'s responsibility and stays there. See
[Compatibility & boundary](#compatibility--boundary-with-prepaid_engine) below.

**Current status**: backend domain + persistence + Load Survey ingestion with sequence-continuity
/ negative-consumption detection and data-quality holds, Daily Load Profile ingestion, a
configurable out-of-range VEE check, meter master data with a temporal installation/replacement
history, EF Core migrations, and 19 passing unit tests. No frontend yet. This is a first vertical
slice, not a complete MDMS.

## Structure

- `backend/` — .NET 8 solution (`MDMS.sln`) — see note on target framework below
  - `MDMS.Api` — ASP.NET Core Web API (controllers, Swagger, migration bootstrap)
  - `MDMS.Application` — use cases (`LoadSurveyIngestionService`), persistence port (`IMdmsDbContext`)
  - `MDMS.Domain` — entities, enums, no external dependencies beyond EF Core's `DbSet<T>` shape
    referenced from `IMdmsDbContext`
  - `MDMS.Infrastructure` — EF Core (PostgreSQL/Npgsql) persistence, entity configurations, migrations
  - `MDMS.Tests` — xUnit tests (EF Core InMemory provider)

**Target framework note**: the requested stack is .NET 10, but only the .NET 8 SDK is available
in this environment (.NET 10 is still pre-GA as of writing). Everything here targets **net8.0**
with EF Core 8.x pinned explicitly (the SDK defaults to EF Core 10.x, which is net10.0-only).
Bumping every `<TargetFramework>` to `net10.0` and the EF Core/Npgsql package versions to their
10.x releases is a mechanical change once you install that SDK — no architectural changes needed.

## Domain model (this slice)

| Entity | Purpose |
|---|---|
| `Customer` | Utility account — thin master record; `AccountNumber` is the shared key with `prepaid_engine`'s `Consumer` |
| `ServicePoint` | A customer's physical point of supply — the stable identity a meter is installed against over time |
| `Meter` | Physical meter asset master data (serial number, phase, lifecycle status) |
| `MeterAssignment` | Temporal audit record binding a meter to a service point — installation, replacement (with closing/opening readings), removal. Guarantees an old meter's cumulative reading is never compared against a new meter's |
| `LoadSurveyInterval` | One 30-minute LS block — shape mirrors `prepaid_engine`'s own entity of the same name so the eventual integration is a near-direct mapping. Carries explicit `Source` (Received/Estimated/Edited/Calculated) and `Quality` (Valid/NegativeConsumption/...) |
| `DailyLoadProfile` | The meter's daily profile at the 00:00 boundary, with a provisional/estimated path for a missing day — mirrors `prepaid_engine`'s DLP shape |
| `DataQualityHold` | An active hold blocking further processing for a meter after a data-quality event (e.g. negative consumption) — equivalent in spirit to `prepaid_engine`'s `MeterBillingControl`, renamed since this is about measurement validity, not billing |
| `MeasurementRangeThreshold` | A configurable min/max plausibility range for LS consumption — global default (`MeterId == null`) or meter-specific override. Configuration, not a hard-coded constant, per MDMS's stated principles |

### Load Survey ingestion

`LoadSurveyIngestionService.IngestAsync` validates a batch of LS blocks:

- Tracks each meter's last-known valid cumulative reading **across the whole in-flight batch**,
  not only already-persisted rows — this specifically avoids a bug `prepaid_engine`'s own README
  documents it hit (two blocks for the same meter in one request, where the second is a genuine
  negative-consumption event that a saved-rows-only check would miss). Covered by
  `IngestAsync_TwoBlocksInOneBatch_SecondNegative_IsDetectedWithinBatch`.
- A cumulative reading lower than the prior interval for the same meter is rejected
  (`NegativeConsumption`) and raises (or reactivates) a `DataQualityHold` for that meter — never
  silently zeroed.
- Idempotent: a duplicate interval (same `MeterId + IntervalStartUtc + IntervalEndUtc`), whether
  already persisted or repeated within the same batch, is a no-op, not an error. The database
  also enforces this uniquely as a second layer.
- A bad item never fails the rest of the batch — matches `prepaid_engine`'s established batch
  pattern (see its conversions/billing-holds-clear-bulk endpoints).

### Daily Load Profile ingestion

`DailyLoadProfileIngestionService.IngestAsync` mirrors `prepaid_engine`'s own DLP behavior
("replaces a provisional profile if one exists"):

- No existing profile for that `ServicePointId + MeterId + ProfileDate` → stores it as `Received`.
- An existing **provisional** (`Estimated`/`Missing`) profile for that key → replaced by the real
  `Received` one; the provisional row is not left behind.
- An existing **`Received`** profile for that key → rejected (`409 Conflict`) rather than
  overwritten — a genuine received value is never silently replaced.

### Out-of-range VEE checks

`OutOfRangeValidationService` is the second VEE check (alongside LS ingestion's inline
negative-consumption detection): a configurable plausibility range for LS consumption, checked
on demand rather than only at ingestion time so a threshold added/corrected later can be applied
retroactively to already-stored data.

- A meter-specific `MeasurementRangeThreshold` takes precedence over the global default
  (`MeterId == null`); a meter with neither configured is never flagged — no threshold means no
  opinion, not a false positive.
- `RunCheckAsync` re-evaluates every currently-`Valid` LS interval for a meter (or every meter
  with a configured threshold, if no meter is specified) and flags any breach `OutOfRange` via
  `LoadSurveyInterval.FlagOutOfRange()` — the consumption value itself is never altered, only the
  quality annotation, so a later re-check after a threshold correction isn't comparing against an
  already-mutated number.
- Not yet wired into `LoadSurveyIngestionService`'s inline ingestion path — today it runs only via
  its own endpoint (see below), not automatically on every ingest. Also LS-only for now; DLP
  out-of-range checking is a documented gap (see "Not yet built").

## Compatibility & boundary with prepaid_engine

`prepaid_engine` (.NET 8, PostgreSQL, 317 tests, real Angular frontend) currently does its own
lightweight LS/DLP ingestion and negative-consumption detection (`LoadSurveyInterval`,
`DailyLoadProfile`, `MeterBillingControl`) because no MDMS existed yet — its own README calls
this out as a stand-in ("a formal VEE service... a real HES/MDM adapter... are explicitly out of
scope for this phase"). MDMS is meant to take over that measurement-side responsibility.

**Stays in `prepaid_engine`, untouched by this work**: `Tariff`/`TariffSlab`/`TouPeriod`,
`ElectricityDuty`, `FppasCharge`, TMC/CPMC, `ArrearRecovery`, `PrepaidWallet`/`WalletTransaction`,
`PrepaidBill`, `RechargeTransaction`, `MeterCommand`, `ConnectivityCommand`, `ConversionRequest`,
`ReconciliationAdjustment`, `OperationalException`, `AuditEntry`, `TariffVersion`, `BillingRun` —
all genuine billing/wallet/tariff/RMS-integration domain logic, verified against MePDCL's real
tariff book. Nothing here duplicates it.

**Moves to MDMS** (this repo, greenfield): meter/service-point/customer master data, meter
installation/replacement history, LS/DLP ingestion and validation, data-quality holds.

**Integration point (not yet built)**: `prepaid_engine` is expected to eventually consume
validated LS/DLP data from MDMS's API instead of ingesting it directly — mirroring the
`IRmsClient`/`MockRmsClient` port pattern it already uses for RMS. That port does not exist in
`prepaid_engine` yet; per your decision, this session left `prepaid_engine`'s code untouched.

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
| `GET /api/v1/vee/thresholds` | List configured out-of-range thresholds (global default first) |
| `POST /api/v1/vee/thresholds` | Configure a plausibility threshold — global (`meterId: null`) or meter-specific |
| `POST /api/v1/vee/out-of-range-checks/run?meterId=` | Re-evaluate stored LS intervals against their effective threshold, flagging breaches `OutOfRange`; omit `meterId` to sweep every meter with a configured threshold |
| `GET /swagger` | Interactive API docs (Development only) |

**No authentication is wired up yet** — this is a local-development skeleton, not intended for
shared/production exposure. Add auth before that changes.

### Testing

```bash
cd backend
dotnet test MDMS.sln
```

**19 tests, all passing** — covering LS ingestion's happy path, consumption-delta calculation,
negative-consumption rejection + hold creation, the in-batch negative-consumption detection
regression, idempotency (duplicate-in-batch and already-persisted duplicate), DLP ingestion (new
profile, provisional replacement, rejecting an overwrite of a received profile), out-of-range
threshold resolution (meter-specific vs. global vs. unconfigured), and the out-of-range check
itself (flagging a breach without mutating the underlying value, ignoring meters with no
threshold, and sweeping every thresholded meter when none is specified).

## Not yet built

- Frontend (Next.js/React/MUI/TanStack Query/ECharts) — deliberately deferred per your "backend
  first" preference.
- Out-of-range checking is not run automatically at ingestion time (only on demand via its
  endpoint) and does not yet cover DLP, only LS.
- A fuller VEE rules engine beyond negative-consumption + out-of-range checks (estimation
  scheduling, provisional-DLP auto-creation when a day's profile never arrives).
- The `IMeterDataClient`-style integration port on the `prepaid_engine` side.
- Authentication/authorization.
- Seed data / demo data.
