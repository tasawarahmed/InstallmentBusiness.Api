using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using InstallmentBusiness.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WithdrawalsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IInvestmentService _investments;
    public WithdrawalsController(AppDbContext db, IInvestmentService investments) { _db = db; _investments = investments; }

    private static WithdrawalResponseDto ToDto(Withdrawal w) => new(
        w.WithdrawalId, w.InvestmentId, w.Amount, w.WithdrawalDate, w.Status, w.Notes);

    [HttpGet]
    public async Task<ActionResult<List<WithdrawalResponseDto>>> List([FromQuery] int? investmentId)
    {
        var query = _db.Withdrawals.AsQueryable();
        if (investmentId.HasValue) query = query.Where(w => w.InvestmentId == investmentId);
        var withdrawals = await query.ToListAsync();
        return withdrawals.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WithdrawalResponseDto>> Get(int id)
    {
        var w = await _db.Withdrawals.FindAsync(id);
        if (w is null) return NotFound(new { error = $"Withdrawal {id} not found." });
        return ToDto(w);
    }

    // Validated against remaining principal (original investment minus
    // withdrawals already Completed). Status defaults to "Completed" --
    // pass "Pending" to defer, then call POST /{id}/complete later.
    [HttpPost]
    public async Task<ActionResult<WithdrawalResponseDto>> Create(CreateWithdrawalDto dto)
    {
        var withdrawal = await _investments.RecordWithdrawalAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = withdrawal.WithdrawalId }, ToDto(withdrawal));
    }

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<WithdrawalResponseDto>> Complete(int id)
    {
        var withdrawal = await _investments.MarkWithdrawalCompletedAsync(id);
        return ToDto(withdrawal);
    }
}
