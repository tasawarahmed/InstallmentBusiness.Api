# InstallmentBusiness.Api

.NET 6 Web API for the installment lending + investor business. Built **database-first** against the schema and triggers already established and verified with live data — this project does not use EF Migrations, and should never have `dotnet ef migrations add` run against it.

## Setup

1. **Point this at any SQL Server database** — a genuinely new/empty one, or one that already has some history with this project. Edit `appsettings.json`:
   - `ConnectionStrings:InstallmentBusiness` → your SQL Server instance.
   - `Jwt:Key` → **replace the placeholder with your own long random secret** before running this anywhere but your own machine. Anyone with this key can mint valid tokens.
2. **If this database already received any of the 6 migration scripts by hand** (true for the original dev database this project was built against), run `BaselineExistingDatabase.sql` once, manually, in SSMS, **before step 3** — see "Database migrations" below for why. Skip this step entirely for a genuinely new/empty database.
3. From this folder:
   ```
   dotnet restore
   dotnet build
   dotnet run
   ```
   On startup, the API automatically applies whatever migration scripts that specific database hasn't received yet — see below. If a script fails, the app deliberately does not start.
4. Open the URL shown in the console + `/swagger` for interactive API docs.

**First login:** on first run, if the `Users` table is empty, the API seeds one account: username `admin`, password `ChangeMe123!`. Log in with `POST /api/auth/login`, then immediately call `POST /api/auth/change-password` with that token — this is a well-known placeholder, not a real secret. To test protected endpoints in Swagger, click **Authorize** and paste just the token (Swagger adds the `Bearer` prefix itself).

## Database migrations

This project's schema history (7 separate scripts, applied by hand over the course of development) is now embedded directly in the compiled API and applied automatically at startup, using [DbUp](https://dbup.readthedocs.io/). You no longer run `.sql` files manually against a new database — deploying the API *is* the migration step.

- **On a database that doesn't exist at all yet**, the API creates it first (via DbUp's `EnsureDatabase.For.SqlDatabase`), then applies every script from scratch — a genuinely new client site needs nothing manual beyond a connection string that names a database, existing or not. (This does mean the SQL login in that connection string needs permission to create databases — e.g. the `dbcreator` server role — not just ownership of one that already exists.)
- **The scripts live in `Migrations/Scripts/`**, numbered `0000` through `0007`, compiled into the assembly as embedded resources (see the `<EmbeddedResource>` entry in the `.csproj`) — a deployment is one self-contained artifact, nothing extra to copy alongside it. `0000` is the original schema that predates this whole project (see the handover document for an important honest caveat about how that one was reconstructed).
- **DbUp tracks what's already run** in its own `SchemaVersions` table (created automatically), keyed by script name. On every startup, only scripts not yet recorded there are applied — in numeric order, each in its own transaction. Once a script is recorded, DbUp never runs it again, regardless of what's in the file.
- **Every script is also independently idempotent** (checks `IF NOT EXISTS` before creating anything) as a second, backup safety net — specifically for the one-time situation below, not something you need to think about on ordinary runs.
- **Adopting an already-migrated database** (like this project's original dev database, which received all 6 scripts by hand before DbUp existed): DbUp's tracking table doesn't know that history yet, so run `BaselineExistingDatabase.sql` once first — it marks all 6 scripts as already applied without re-running them. Do this exactly once, before ever pointing the DbUp-enabled API at that specific database for the first time.
- **Adding a new migration in the future:** drop a new `0008_....sql` file into `Migrations/Scripts/`, write it defensively (`IF NOT EXISTS` guards, following the existing scripts as examples), and it'll apply automatically the next time the API starts against any database that doesn't have it yet — including every existing client site, with no manual SSMS work required.
- **The original, un-numbered `.sql` files** (`InstallmentSchema_Migration.sql` and friends) are kept only as historical documentation of how each piece was actually built and debugged — don't run them manually against any database going forward; the numbered scripts in `Migrations/Scripts/` are the current, authoritative source.

## Authentication

Every endpoint requires a valid JWT **except** `POST /api/auth/login`. There are no roles or permission tiers yet — any logged-in account can do anything. Create additional accounts (for other staff) via `POST /api/auth/register`, which itself requires being logged in already — there is no open public signup.

`GET /api/auth/me` returns the current user's display name — useful for a frontend to show "logged in as ..." and to verify a stored token is still valid without decoding it client-side.

## Architecture

- **`Data/AppDbContext.cs`** — Fluent API mappings matching the exact schema (precision, max lengths, keys, view mappings). Nine reporting views are mapped as keyless entities — the API never recomputes an aggregate the database already computes.
- **`Services/ProfitCalculator.cs`** — the one place the profit-split formula lives: `ProfitRate = ((DownPayment + TotalPayable) − ProductCostPrice) / (DownPayment + TotalPayable)`, applied identically to the down payment and every installment.
- **`Services/PlanService.cs`** → `FinalizeAsync` — validates ≥1 guarantor, creates Installment 0 (the down payment) and the full schedule, all in one DB transaction.
- **`Services/PaymentService.cs`** → `RecordPaymentAsync` — allocates one payment across as many pending installments as it covers (this is how an advance/overpayment is represented — no separate "credit balance" concept).
- **`Middleware/ExceptionHandlingMiddleware.cs`** — maps `KeyNotFoundException`→404, `ArgumentException`/`InvalidOperationException`→400, everything else→500, so controllers stay free of repetitive try/catch.

### A pattern worth knowing before you extend this code

Whenever a `PaymentTransaction` is inserted, a database trigger updates the related `InstallmentPayment` row (and `CashLedger`) as a side effect. EF Core's change tracker does **not** know about that automatically — both `FinalizeAsync` and `RecordPaymentAsync` explicitly call `.ReloadAsync()` on the affected installment right after `SaveChangesAsync()`, before computing the cost/profit split. If you add new code that touches `PaymentTransactions` directly, keep this reload in place, or the cost/profit split will be computed against stale data.

## Known limitations (flagged deliberately, not fixed silently)

- **Removing a plan's only guarantor after it's `Active` doesn't un-finalize it.** The DB trigger only guards the transition *into* `Active`.
- **A payment larger than the total remaining schedule is rejected**, not partially applied with a credit balance. Early full payoff works fine as long as the amount doesn't exceed the sum of what's still owed; anything beyond that needs a decision on how you want early-payoff credit represented.
- **If the DB-level guarantor-guard trigger is what rejects a finalize** (rather than the API's own check, which runs first) the error surfaces as a generic 500, not a friendly 400 — this should be rare in practice, since the API validates the same rule before ever reaching the trigger.
- **No reversal handling** if a `Paid` profit payment or `Completed` withdrawal is later corrected — flagged when the triggers were first built, still open.
