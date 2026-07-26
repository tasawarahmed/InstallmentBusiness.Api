using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using InstallmentBusiness.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstallmentPlansController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPlanService _plans;
    public InstallmentPlansController(AppDbContext db, IPlanService plans) { _db = db; _plans = plans; }

    private static PlanResponseDto ToDto(InstallmentPlan p) => new(
        p.PlanId, p.CustomerId, p.Customer?.FirstName + " " + p.Customer?.LastName,
        p.ProductId, p.Product?.ProductName ?? "",
        p.ProductSalePrice, p.ProductCostPrice, p.DownPayment, p.LoanAmount,
        p.TenureMonths, p.MonthlyInstallment, p.TotalPayable,
        p.StartDate, p.EndDate, p.Status, p.PlanGuarantors?.Count ?? 0);

    private static InstallmentScheduleItemDto ToScheduleDto(InstallmentPayment i) => new(
        i.PaymentId, i.InstallmentNumber, i.AmountDue, i.AmountPaid ?? 0, i.Outstanding,
        i.DueDate, i.PaidDate, i.Status, i.CostRecoveryAmount ?? 0, i.ProfitAmount ?? 0);

    [HttpGet]
    public async Task<ActionResult<List<PlanResponseDto>>> List([FromQuery] string? status)
    {
        var plans = await _plans.ListAsync(status);
        return plans.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlanResponseDto>> Get(int id)
    {
        var plan = await _plans.GetByIdAsync(id);
        if (plan is null) return NotFound(new { error = $"Plan {id} not found." });
        return ToDto(plan);
    }

    [HttpGet("{id:int}/schedule")]
    public async Task<ActionResult<List<InstallmentScheduleItemDto>>> GetSchedule(int id)
    {
        var exists = await _db.InstallmentPlans.AnyAsync(p => p.PlanId == id);
        if (!exists) return NotFound(new { error = $"Plan {id} not found." });

        var schedule = await _plans.GetScheduleAsync(id);
        return schedule.Select(ToScheduleDto).ToList();
    }

    // Changes an installment's due date -- e.g. the customer asked for more
    // time. Blocked once that installment is Paid or Waived; not blocked
    // from creating an out-of-order schedule (see PlanService for why).
    [HttpPost("{id:int}/schedule/{paymentId:int}/reschedule")]
    public async Task<ActionResult<InstallmentScheduleItemDto>> Reschedule(int id, int paymentId, RescheduleInstallmentDto dto)
    {
        var installment = await _plans.RescheduleInstallmentAsync(id, paymentId, dto.NewDueDate, dto.Reason);
        return ToScheduleDto(installment);
    }

    // Creates a plan in 'Proposed' status. No installment schedule exists
    // yet -- that's only generated on Finalize.
    [HttpPost("propose")]
    public async Task<ActionResult<PlanResponseDto>> Propose(CreatePlanProposalDto dto)
    {
        var plan = await _plans.CreateProposalAsync(dto);
        await _db.Entry(plan).Reference(p => p.Customer).LoadAsync();
        await _db.Entry(plan).Reference(p => p.Product).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = plan.PlanId }, ToDto(plan));
    }

    [HttpPost("{id:int}/guarantors")]
    public async Task<IActionResult> AddGuarantor(int id, AddGuarantorToPlanDto dto)
    {
        await _plans.AddGuarantorAsync(id, dto.GuarantorId);
        return NoContent();
    }

    [HttpDelete("{id:int}/guarantors/{guarantorId:int}")]
    public async Task<IActionResult> RemoveGuarantor(int id, int guarantorId)
    {
        await _plans.RemoveGuarantorAsync(id, guarantorId);
        return NoContent();
    }

    // Validates >=1 guarantor, generates the down payment (Installment 0)
    // plus the full schedule, and moves the plan to 'Active'. This is the
    // one place a plan's installment schedule and cash flow begin.
    [HttpPost("{id:int}/finalize")]
    public async Task<ActionResult<PlanResponseDto>> Finalize(int id, FinalizePlanDto dto)
    {
        var plan = await _plans.FinalizeAsync(id, dto);
        await _db.Entry(plan).Reference(p => p.Customer).LoadAsync();
        await _db.Entry(plan).Reference(p => p.Product).LoadAsync();
        await _db.Entry(plan).Collection(p => p.PlanGuarantors).LoadAsync();
        return ToDto(plan);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _plans.CancelAsync(id);
        return NoContent();
    }
}
