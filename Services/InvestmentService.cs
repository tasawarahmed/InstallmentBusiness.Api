using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Services;

public interface IInvestmentService
{
    Task<Investment> RecordInvestmentAsync(CreateInvestmentDto dto);
    Task<PlanFunding> AllocateFundingAsync(AllocateFundingDto dto);
    Task<ProfitPayment> RecordProfitPaymentAsync(CreateProfitPaymentDto dto);
    Task<ProfitPayment> MarkProfitPaymentPaidAsync(int profitPaymentId, string? paymentMethod);
    Task<Withdrawal> RecordWithdrawalAsync(CreateWithdrawalDto dto);
    Task<Withdrawal> MarkWithdrawalCompletedAsync(int withdrawalId);
}

public class InvestmentService : IInvestmentService
{
    private readonly AppDbContext _db;
    public InvestmentService(AppDbContext db) => _db = db;

    public async Task<Investment> RecordInvestmentAsync(CreateInvestmentDto dto)
    {
        var investor = await _db.Investors.FindAsync(dto.InvestorId)
            ?? throw new KeyNotFoundException($"Investor {dto.InvestorId} not found.");
        if (dto.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var investment = new Investment
        {
            InvestorId = dto.InvestorId,
            Amount = dto.Amount,
            InvestmentDate = dto.InvestmentDate,
            ProfitRate = dto.ProfitRate ?? investor.DefaultProfitRate,
            Status = "Active",
            MaturityDate = dto.MaturityDate,
            CreatedAt = DateTime.UtcNow
        };
        _db.Investments.Add(investment);
        await _db.SaveChangesAsync(); // fires trg_Investments_CashIn
        return investment;
    }

    public async Task<PlanFunding> AllocateFundingAsync(AllocateFundingDto dto)
    {
        _ = await _db.InstallmentPlans.FindAsync(dto.PlanId)
            ?? throw new KeyNotFoundException($"Plan {dto.PlanId} not found.");
        _ = await _db.Investments.FindAsync(dto.InvestmentId)
            ?? throw new KeyNotFoundException($"Investment {dto.InvestmentId} not found.");
        if (dto.AmountAllocated <= 0)
            throw new ArgumentException("AmountAllocated must be greater than zero.");

        var funding = new PlanFunding
        {
            PlanId = dto.PlanId,
            InvestmentId = dto.InvestmentId,
            AmountAllocated = dto.AmountAllocated,
            CreatedAt = DateTime.UtcNow
        };
        _db.PlanFundings.Add(funding);
        await _db.SaveChangesAsync(); // UQ_PlanFunding_PlanInvestment stops the same investment being allocated to a plan twice
        return funding;
    }

    public async Task<ProfitPayment> RecordProfitPaymentAsync(CreateProfitPaymentDto dto)
    {
        _ = await _db.Investments.FindAsync(dto.InvestmentId)
            ?? throw new KeyNotFoundException($"Investment {dto.InvestmentId} not found.");
        if (dto.ProfitAmount <= 0)
            throw new ArgumentException("ProfitAmount must be greater than zero.");

        var status = string.IsNullOrWhiteSpace(dto.Status) ? "Paid" : dto.Status;

        var payment = new ProfitPayment
        {
            InvestmentId = dto.InvestmentId,
            ProfitAmount = dto.ProfitAmount,
            PaymentDate = dto.PaymentDate,
            PaymentMethod = dto.PaymentMethod,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        _db.ProfitPayments.Add(payment);
        // fires trg_ProfitPayments_CashOut_Insert IF status == "Paid"; a "Pending"
        // row reaches CashLedger later, exactly once, via MarkProfitPaymentPaidAsync.
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<ProfitPayment> MarkProfitPaymentPaidAsync(int profitPaymentId, string? paymentMethod)
    {
        var payment = await _db.ProfitPayments.FindAsync(profitPaymentId)
            ?? throw new KeyNotFoundException($"ProfitPayment {profitPaymentId} not found.");
        if (payment.Status == "Paid")
            throw new InvalidOperationException("This profit payment is already marked Paid.");

        payment.Status = "Paid";
        if (!string.IsNullOrWhiteSpace(paymentMethod)) payment.PaymentMethod = paymentMethod;
        await _db.SaveChangesAsync(); // fires trg_ProfitPayments_CashOut_Update
        return payment;
    }

    public async Task<Withdrawal> RecordWithdrawalAsync(CreateWithdrawalDto dto)
    {
        var investment = await _db.Investments.FindAsync(dto.InvestmentId)
            ?? throw new KeyNotFoundException($"Investment {dto.InvestmentId} not found.");
        if (dto.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var alreadyWithdrawn = await _db.Withdrawals
            .Where(w => w.InvestmentId == dto.InvestmentId && w.Status == "Completed")
            .SumAsync(w => (decimal?)w.Amount) ?? 0;

        if (alreadyWithdrawn + dto.Amount > investment.Amount)
            throw new InvalidOperationException(
                $"Withdrawal of {dto.Amount} would exceed remaining principal " +
                $"({investment.Amount - alreadyWithdrawn} of {investment.Amount} original investment).");

        var status = string.IsNullOrWhiteSpace(dto.Status) ? "Completed" : dto.Status;

        var withdrawal = new Withdrawal
        {
            InvestmentId = dto.InvestmentId,
            Amount = dto.Amount,
            WithdrawalDate = dto.WithdrawalDate,
            Status = status,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
        _db.Withdrawals.Add(withdrawal);
        // fires trg_Withdrawals_CashOut_Insert IF status == "Completed"; a "Pending"
        // row reaches CashLedger later, exactly once, via MarkWithdrawalCompletedAsync.
        await _db.SaveChangesAsync();
        return withdrawal;
    }

    public async Task<Withdrawal> MarkWithdrawalCompletedAsync(int withdrawalId)
    {
        var withdrawal = await _db.Withdrawals.FindAsync(withdrawalId)
            ?? throw new KeyNotFoundException($"Withdrawal {withdrawalId} not found.");
        if (withdrawal.Status == "Completed")
            throw new InvalidOperationException("This withdrawal is already marked Completed.");

        withdrawal.Status = "Completed";
        await _db.SaveChangesAsync(); // fires trg_Withdrawals_CashOut_Update
        return withdrawal;
    }
}
