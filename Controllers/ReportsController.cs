using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
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

    // The raw transaction list behind the monthly aggregate above -- filterable
    // and paginated, since this table grows by one row on every single cash
    // movement for the life of the business and can't be assumed to stay small.
    // Note: startDate/endDate/transactionType/direction are plain filters, not
    // validated against the underlying CHECK-constraint values -- an
    // unrecognized value just matches zero rows, same as every other optional
    // filter elsewhere in this API.
    [HttpGet("cash-ledger")]
    public async Task<ActionResult<PagedCashLedgerResultDto>> CashLedger(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? transactionType,
        [FromQuery] string? direction,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 500) pageSize = 50;

        var query = _db.CashLedgerEntries.AsQueryable();
        if (startDate.HasValue) query = query.Where(c => c.TransactionDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(c => c.TransactionDate <= endDate.Value);
        if (!string.IsNullOrWhiteSpace(transactionType)) query = query.Where(c => c.TransactionType == transactionType);
        if (!string.IsNullOrWhiteSpace(direction)) query = query.Where(c => c.Direction == direction);

        var totalCount = await query.CountAsync();

        // Constructing the DTO directly here (not via a helper method) is
        // deliberate -- this keeps the projection translatable to SQL so
        // Skip/Take run as a real server-side OFFSET/FETCH, not by pulling
        // the whole table into memory first. See ReportDtos.cs / the bug
        // history in the handover document for why this distinction matters.
        var items = await query
            .OrderByDescending(c => c.TransactionDate)
            .ThenByDescending(c => c.LedgerId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CashLedgerEntryDto(
                c.LedgerId, c.TransactionDate, c.TransactionType, c.Direction,
                c.Amount, c.ReferenceTable, c.ReferenceId, c.Notes, c.CreatedAt))
            .ToListAsync();

        return new PagedCashLedgerResultDto(items, totalCount, page, pageSize);
    }

    // One line per customer payment (down payment or installment) in a
    // given period, with cost-recovery/profit split per line, plus totals
    // across the whole filtered range (not just the current page) so a
    // totals row can be rendered under the table regardless of pagination.
    [HttpGet("customer-payments")]
    public async Task<ActionResult<CustomerPaymentsReportDto>> CustomerPayments(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? customerId,
        [FromQuery] int? planId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 500) pageSize = 50;

        var query = _db.CustomerPayments.AsQueryable();
        if (startDate.HasValue) query = query.Where(c => c.TransactionDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(c => c.TransactionDate <= endDate.Value);
        if (customerId.HasValue) query = query.Where(c => c.CustomerId == customerId);
        if (planId.HasValue) query = query.Where(c => c.PlanId == planId);

        var totalCount = await query.CountAsync();
        var totalCostRecovery = await query.SumAsync(c => c.CostRecoveryAmount);
        var totalProfit = await query.SumAsync(c => c.ProfitAmount);
        var totalPayments = await query.SumAsync(c => c.TotalPayment);

        var items = await query
            .OrderBy(c => c.TransactionDate)
            .ThenBy(c => c.TransactionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerPaymentLineDto(
                c.TransactionId, c.TransactionDate, c.PlanId, c.CustomerName,
                c.InstallmentNumber, c.CostRecoveryAmount, c.ProfitAmount, c.TotalPayment))
            .ToListAsync();

        return new CustomerPaymentsReportDto(
            items, totalCount, page, pageSize, totalCostRecovery, totalProfit, totalPayments);
    }

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

