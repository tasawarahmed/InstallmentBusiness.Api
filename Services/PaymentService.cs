using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Services;

public interface IPaymentService
{
    Task<List<PaymentTransaction>> RecordPaymentAsync(
        int planId, decimal amount, DateTime transactionDate,
        string? paymentMethod, string? referenceNo, string? receivedBy);
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    public PaymentService(AppDbContext db) => _db = db;

    // Allocates one incoming payment across as many pending installments
    // (oldest first) as it covers -- this is how both a normal payment and
    // an "advance" payment (requirement 3) are represented, with no new
    // schema concept: a large payment simply becomes multiple
    // PaymentTransactions rows, each tied to the installment it settles.
    public async Task<List<PaymentTransaction>> RecordPaymentAsync(
        int planId, decimal amount, DateTime transactionDate,
        string? paymentMethod, string? referenceNo, string? receivedBy)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        using var tx = await _db.Database.BeginTransactionAsync();

        var plan = await _db.InstallmentPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (plan.Status != "Active")
            throw new InvalidOperationException(
                $"Cannot record a payment against a plan that is not Active (current status: {plan.Status}).");

        // Installment 0 (the down payment) is deliberately excluded here --
        // it is only ever settled once, inside PlanService.FinalizeAsync.
        var pending = await _db.InstallmentPayments
            .Where(p => p.PlanId == planId && p.InstallmentNumber >= 1
                     && (p.Status == "Pending" || p.Status == "PartiallyPaid" || p.Status == "Overdue"))
            .OrderBy(p => p.InstallmentNumber)
            .ToListAsync();

        var totalOutstanding = pending.Sum(p => p.Outstanding);
        if (amount > totalOutstanding)
            throw new InvalidOperationException(
                $"Amount ({amount}) exceeds the total outstanding balance ({totalOutstanding}) across all " +
                "scheduled installments. Recording a payment larger than the remaining schedule isn't supported yet.");

        var profitRate = ProfitCalculator.CalculateProfitRate(plan);
        var remaining = amount;
        var createdTransactions = new List<PaymentTransaction>();

        foreach (var installment in pending)
        {
            if (remaining <= 0) break;

            var allocate = Math.Min(remaining, installment.Outstanding);
            if (allocate <= 0) continue;

            var txn = new PaymentTransaction
            {
                PlanId = planId,
                PaymentId = installment.PaymentId,
                Installment = installment, // set explicitly rather than relying on EF's relationship fixup
                AmountReceived = allocate,
                TransactionDate = transactionDate,
                PaymentMethod = paymentMethod,
                ReferenceNo = referenceNo,
                ReceivedBy = receivedBy,
                CreatedAt = DateTime.UtcNow
            };
            _db.PaymentTransactions.Add(txn);
            await _db.SaveChangesAsync(); // fires trg_PaymentTransactions_SyncInstallment + trg_PaymentTransactions_CashIn

            // Same reload requirement as PlanService.FinalizeAsync -- the
            // trigger updated AmountPaid/Status/PaidDate at the DB level;
            // EF's tracked copy needs an explicit reload to see it.
            await _db.Entry(installment).ReloadAsync();

            var (profitPortion, costPortion) = ProfitCalculator.Split(allocate, profitRate);
            installment.CostRecoveryAmount = (installment.CostRecoveryAmount ?? 0) + costPortion;
            installment.ProfitAmount = (installment.ProfitAmount ?? 0) + profitPortion;
            await _db.SaveChangesAsync();

            createdTransactions.Add(txn);
            remaining -= allocate;
        }

        await tx.CommitAsync();
        return createdTransactions;
    }
}
