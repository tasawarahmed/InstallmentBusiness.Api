using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using InstallmentBusiness.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfitPaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IInvestmentService _investments;
    public ProfitPaymentsController(AppDbContext db, IInvestmentService investments) { _db = db; _investments = investments; }

    private static ProfitPaymentResponseDto ToDto(ProfitPayment p) => new(
        p.ProfitPaymentId, p.InvestmentId, p.ProfitAmount, p.PaymentDate, p.PaymentMethod, p.Status);

    [HttpGet]
    public async Task<ActionResult<List<ProfitPaymentResponseDto>>> List([FromQuery] int? investmentId)
    {
        var query = _db.ProfitPayments.AsQueryable();
        if (investmentId.HasValue) query = query.Where(p => p.InvestmentId == investmentId);
        var payments = await query.ToListAsync();
        return payments.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProfitPaymentResponseDto>> Get(int id)
    {
        var p = await _db.ProfitPayments.FindAsync(id);
        if (p is null) return NotFound(new { error = $"ProfitPayment {id} not found." });
        return ToDto(p);
    }

    // Status defaults to "Paid" if omitted -- fires the Insert cash-out
    // trigger immediately. Pass Status="Pending" to defer, then call
    // POST /{id}/mark-paid later -- either path reaches CashLedger exactly once.
    [HttpPost]
    public async Task<ActionResult<ProfitPaymentResponseDto>> Create(CreateProfitPaymentDto dto)
    {
        var payment = await _investments.RecordProfitPaymentAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = payment.ProfitPaymentId }, ToDto(payment));
    }

    [HttpPost("{id:int}/mark-paid")]
    public async Task<ActionResult<ProfitPaymentResponseDto>> MarkPaid(int id, MarkProfitPaymentPaidDto dto)
    {
        var payment = await _investments.MarkProfitPaymentPaidAsync(id, dto.PaymentMethod);
        return ToDto(payment);
    }
}
