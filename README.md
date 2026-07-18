# InstallmentBusiness.Api

.NET 6 Web API for the installment lending + investor business. Built **database-first** against the schema and triggers already established and verified with live data — this project does not use EF Migrations, and should never have `dotnet ef migrations add` run against it.

## Setup

1. Make sure every migration script has been run against your SQL Server database, in this order:
   - Original schema
   - `InstallmentSchema_Migration.sql` (PaymentTransactions, PlanFunding, Withdrawals, CashLedger)
   - `InstallmentSchema_Triggers.sql`
   - `InstallmentSchema_Migration_Guarantors_Idempotent.sql` (PlanGuarantors, Proposed status, finalize-guard triggers)
   - `InstallmentSchema_Migration_CostPriceSnapshot.sql` (frozen `ProductCostPrice`)
   - `InstallmentSchema_Migration_Users.sql` (Users table, backs JWT auth)
2. Edit `appsettings.json`:
   - `ConnectionStrings:InstallmentBusiness` → your SQL Server instance.
   - `Jwt:Key` → **replace the placeholder with your own long random secret** before running this anywhere but your own machine. Anyone with this key can mint valid tokens.
3. From this folder:
   ```
   dotnet restore
   dotnet build
   dotnet run
   ```
4. Open the URL shown in the console + `/swagger` for interactive API docs.

**First login:** on first run, if the `Users` table is empty, the API seeds one account: username `admin`, password `ChangeMe123!`. Log in with `POST /api/auth/login`, then immediately call `POST /api/auth/change-password` with that token — this is a well-known placeholder, not a real secret. To test protected endpoints in Swagger, click **Authorize** and paste just the token (Swagger adds the `Bearer` prefix itself).

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
