using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.Models.Views;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

// Every action here is a thin, read-only pass-through to a database view.
// Aggregates are never recomputed in C# -- the views (built and verified
// earlier) remain the single source of truth for every reported figure.
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("cash-in-hand")]
    public async Task<ActionResult<decimal>> CashInHand()
    {
        var row = await _db.CashInHand.FirstOrDefaultAsync();
        return row?.CashInHandAmount ?? 0m;
    }

    [HttpGet("cash-ledger-by-period")]
    public async Task<ActionResult<List<CashLedgerByPeriod>>> CashLedgerByPeriod() =>
        await _db.CashLedgerByPeriod.OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();

    [HttpGet("plan-summary")]
    public async Task<ActionResult<List<PlanSummary>>> PlanSummary() =>
        await _db.PlanSummaries.OrderBy(x => x.PlanId).ToListAsync();

    [HttpGet("plan-summary/{planId:int}")]
    public async Task<ActionResult<PlanSummary>> PlanSummaryById(int planId)
    {
        var summary = await _db.PlanSummaries.FirstOrDefaultAsync(x => x.PlanId == planId);
        if (summary is null) return NotFound(new { error = $"Plan {planId} not found." });
        return summary;
    }

    [HttpGet("pending-installments")]
    public async Task<ActionResult<List<PendingInstallment>>> PendingInstallments([FromQuery] int? planId)
    {
        var query = _db.PendingInstallments.AsQueryable();
        if (planId.HasValue) query = query.Where(x => x.PlanId == planId);
        return await query.OrderBy(x => x.DueDate).ToListAsync();
    }

    [HttpGet("profit-by-period")]
    public async Task<ActionResult<List<ProfitByPeriod>>> ProfitByPeriod() =>
        await _db.ProfitByPeriod.OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();

    [HttpGet("investor-summary")]
    public async Task<ActionResult<List<InvestorSummary>>> InvestorSummary() =>
        await _db.InvestorSummaries.OrderBy(x => x.InvestorId).ToListAsync();

    [HttpGet("investor-ledger")]
    public async Task<ActionResult<List<InvestorLedgerEntry>>> InvestorLedger([FromQuery] int? investorId)
    {
        var query = _db.InvestorLedger.AsQueryable();
        if (investorId.HasValue) query = query.Where(x => x.InvestorId == investorId);
        return await query.OrderBy(x => x.InvestorId).ToListAsync();
    }

    [HttpGet("plan-funding-summary")]
    public async Task<ActionResult<List<PlanFundingSummary>>> PlanFundingSummary([FromQuery] int? planId)
    {
        var query = _db.PlanFundingSummaries.AsQueryable();
        if (planId.HasValue) query = query.Where(x => x.PlanId == planId);
        return await query.OrderBy(x => x.PlanId).ToListAsync();
    }

    [HttpGet("guarantor-plan-count")]
    public async Task<ActionResult<List<GuarantorPlanCount>>> GuarantorPlanCount() =>
        await _db.GuarantorPlanCounts.OrderBy(x => x.GuarantorId).ToListAsync();
}
