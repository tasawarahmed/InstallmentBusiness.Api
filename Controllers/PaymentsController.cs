using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using InstallmentBusiness.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/plans/{planId:int}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    public PaymentsController(IPaymentService payments) => _payments = payments;

    private static PaymentTransactionResponseDto ToDto(PaymentTransaction t) => new(
        t.TransactionId, t.PlanId, t.PaymentId, t.Installment?.InstallmentNumber,
        t.AmountReceived, t.TransactionDate, t.PaymentMethod, t.ReferenceNo);

    // Amount may exceed a single installment -- it is allocated across as
    // many upcoming pending installments as it covers (requirement 3:
    // advance payments). The response lists every installment it touched.
    [HttpPost]
    public async Task<ActionResult<RecordPaymentResultDto>> Record(int planId, RecordPaymentDto dto)
    {
        var created = await _payments.RecordPaymentAsync(
            planId, dto.Amount, dto.TransactionDate, dto.PaymentMethod, dto.ReferenceNo, dto.ReceivedBy);

        var allocated = created.Sum(t => t.AmountReceived);
        var result = new RecordPaymentResultDto(
            dto.Amount, created.Select(ToDto).ToList(), dto.Amount - allocated);
        return Ok(result);
    }
}
