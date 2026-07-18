using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Services;

public interface IPlanService
{
    Task<InstallmentPlan> CreateProposalAsync(CreatePlanProposalDto dto);
    Task AddGuarantorAsync(int planId, int guarantorId);
    Task RemoveGuarantorAsync(int planId, int guarantorId);
    Task<InstallmentPlan> FinalizeAsync(int planId, FinalizePlanDto dto);
    Task CancelAsync(int planId);
    Task<InstallmentPlan?> GetByIdAsync(int planId);
    Task<List<InstallmentPlan>> ListAsync(string? status);
    Task<List<InstallmentPayment>> GetScheduleAsync(int planId);
}

public class PlanService : IPlanService
{
    private readonly AppDbContext _db;
    public PlanService(AppDbContext db) => _db = db;

    public async Task<InstallmentPlan> CreateProposalAsync(CreatePlanProposalDto dto)
    {
        var product = await _db.Products.FindAsync(dto.ProductId)
            ?? throw new KeyNotFoundException($"Product {dto.ProductId} not found.");
        _ = await _db.Customers.FindAsync(dto.CustomerId)
            ?? throw new KeyNotFoundException($"Customer {dto.CustomerId} not found.");

        if (dto.TenureMonths < 6 || dto.TenureMonths > 30)
            throw new ArgumentException("TenureMonths must be between 6 and 30 (matches the database constraint).");
        if (dto.DownPayment < 0)
            throw new ArgumentException("DownPayment cannot be negative.");
        if (dto.MonthlyInstallment <= 0)
            throw new ArgumentException("MonthlyInstallment must be greater than zero.");

        var expectedTotal = dto.MonthlyInstallment * dto.TenureMonths;
        if (Math.Abs(expectedTotal - dto.TotalPayable) > 1.00m)
            throw new ArgumentException(
                $"TotalPayable ({dto.TotalPayable}) doesn't match MonthlyInstallment x TenureMonths ({expectedTotal}).");

        // ProductSalePrice and ProductCostPrice are frozen here, at proposal
        // time, from the product's CURRENT prices. Never re-read afterward,
        // even if the product's prices change later.
        var plan = new InstallmentPlan
        {
            CustomerId = dto.CustomerId,
            ProductId = dto.ProductId,
            ProductSalePrice = product.SalePrice,
            ProductCostPrice = product.CostPrice,
            DownPayment = dto.DownPayment,
            LoanAmount = product.SalePrice - dto.DownPayment,
            TenureMonths = dto.TenureMonths,
            MonthlyInstallment = dto.MonthlyInstallment,
            TotalPayable = dto.TotalPayable,
            StartDate = dto.StartDate,
            EndDate = dto.StartDate.AddMonths(dto.TenureMonths),
            Status = "Proposed",
            ApprovedBy = dto.ApprovedBy,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.InstallmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }

    public async Task AddGuarantorAsync(int planId, int guarantorId)
    {
        _ = await _db.InstallmentPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");
        _ = await _db.Guarantors.FindAsync(guarantorId)
            ?? throw new KeyNotFoundException($"Guarantor {guarantorId} not found.");

        var exists = await _db.PlanGuarantors.AnyAsync(pg => pg.PlanId == planId && pg.GuarantorId == guarantorId);
        if (exists) return; // idempotent -- adding the same guarantor twice is a no-op, not an error

        _db.PlanGuarantors.Add(new PlanGuarantor { PlanId = planId, GuarantorId = guarantorId, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveGuarantorAsync(int planId, int guarantorId)
    {
        var pg = await _db.PlanGuarantors.FirstOrDefaultAsync(x => x.PlanId == planId && x.GuarantorId == guarantorId)
            ?? throw new KeyNotFoundException("That guarantor is not linked to this plan.");

        _db.PlanGuarantors.Remove(pg);
        await _db.SaveChangesAsync();
        // Note: removing a plan's only guarantor after it is already Active
        // does NOT retroactively un-finalize it -- the DB trigger only
        // guards the transition INTO Active, not ongoing state.
    }

    public async Task<InstallmentPlan> FinalizeAsync(int planId, FinalizePlanDto dto)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var plan = await _db.InstallmentPlans
            .Include(p => p.PlanGuarantors)
            .FirstOrDefaultAsync(p => p.PlanId == planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (plan.Status != "Proposed")
            throw new InvalidOperationException(
                $"Only a Proposed plan can be finalized (current status: {plan.Status}).");

        if (plan.PlanGuarantors.Count == 0)
            throw new InvalidOperationException(
                "Cannot finalize a plan without at least one guarantor. Call AddGuarantor first.");

        var profitRate = ProfitCalculator.CalculateProfitRate(plan);

        // ── Installment 0: the down payment ─────────────────────────────
        var downPayment = new InstallmentPayment
        {
            PlanId = plan.PlanId,
            InstallmentNumber = 0,
            AmountDue = plan.DownPayment,
            AmountPaid = 0,
            PenaltyAmount = 0,
            CostRecoveryAmount = 0,
            ProfitAmount = 0,
            DueDate = plan.StartDate,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _db.InstallmentPayments.Add(downPayment);
        await _db.SaveChangesAsync();

        if (plan.DownPayment > 0)
        {
            _db.PaymentTransactions.Add(new PaymentTransaction
            {
                PlanId = plan.PlanId,
                PaymentId = downPayment.PaymentId,
                AmountReceived = plan.DownPayment,
                TransactionDate = plan.StartDate,
                PaymentMethod = dto.DownPaymentMethod,
                ReferenceNo = dto.DownPaymentReferenceNo,
                Notes = "Down payment collected at finalization",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(); // fires trg_PaymentTransactions_SyncInstallment + trg_PaymentTransactions_CashIn

            // The two triggers above updated AmountPaid/Status/PaidDate on
            // `downPayment` at the database level -- EF's tracked copy does
            // NOT know about that automatically. Reload before touching it again.
            await _db.Entry(downPayment).ReloadAsync();

            var (profitPortion, costPortion) = ProfitCalculator.Split(plan.DownPayment, profitRate);
            downPayment.CostRecoveryAmount = costPortion;
            downPayment.ProfitAmount = profitPortion;
            await _db.SaveChangesAsync();
        }

        // ── Installments 1..TenureMonths ─────────────────────────────────
        for (var n = 1; n <= plan.TenureMonths; n++)
        {
            _db.InstallmentPayments.Add(new InstallmentPayment
            {
                PlanId = plan.PlanId,
                InstallmentNumber = n,
                AmountDue = plan.MonthlyInstallment,
                AmountPaid = 0,
                PenaltyAmount = 0,
                CostRecoveryAmount = 0,
                ProfitAmount = 0,
                DueDate = plan.StartDate.AddMonths(n),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        // ── Finalize ──────────────────────────────────────────────────────
        plan.Status = "Active";
        await _db.SaveChangesAsync(); // backstopped by trg_InstallmentPlans_RequireGuarantor_Update

        await tx.CommitAsync();
        return plan;
    }

    public async Task CancelAsync(int planId)
    {
        var plan = await _db.InstallmentPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (plan.Status is "Completed")
            throw new InvalidOperationException("A completed plan cannot be cancelled.");

        plan.Status = "Cancelled";
        await _db.SaveChangesAsync();
    }

    public async Task<InstallmentPlan?> GetByIdAsync(int planId) =>
        await _db.InstallmentPlans
            .Include(p => p.Customer)
            .Include(p => p.Product)
            .Include(p => p.PlanGuarantors)
            .FirstOrDefaultAsync(p => p.PlanId == planId);

    public async Task<List<InstallmentPlan>> ListAsync(string? status)
    {
        var query = _db.InstallmentPlans
            .Include(p => p.Customer)
            .Include(p => p.Product)
            .Include(p => p.PlanGuarantors)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);
        return await query.OrderByDescending(p => p.PlanId).ToListAsync();
    }

    public async Task<List<InstallmentPayment>> GetScheduleAsync(int planId) =>
        await _db.InstallmentPayments
            .Where(p => p.PlanId == planId)
            .OrderBy(p => p.InstallmentNumber)
            .ToListAsync();
}
