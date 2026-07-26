namespace InstallmentBusiness.Api.Models.Views;

// All classes below map to database VIEWS as EF Core "keyless entities" --
// read-only, no PK, no tracking. Mapped with .ToView(...) in AppDbContext.
// These are the single source of truth for reporting; the API never
// recomputes these aggregates in C#.

public class PlanSummary
{
    public int PlanId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal LoanAmount { get; set; }
    public int TenureMonths { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal DownPayment { get; set; }
    public decimal TotalPayable { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal BalanceRemaining { get; set; }
    public int OverdueInstallments { get; set; }
    public string? Status { get; set; }
}

public class InvestorSummary
{
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = null!;
    public decimal TotalInvested { get; set; }
    public decimal ActiveInvestment { get; set; }
    public decimal TotalWithdrawn { get; set; }
    public decimal TotalProfitPaid { get; set; }
    public int ActiveInvestments { get; set; }
}

public class InvestorLedgerEntry
{
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = null!;
    public int InvestmentId { get; set; }
    public decimal InvestedAmount { get; set; }
    public DateTime InvestmentDate { get; set; }
    public decimal TotalProfitPaid { get; set; }
    public decimal TotalWithdrawn { get; set; }
    public decimal RemainingPrincipal { get; set; }
    public string? Status { get; set; }
}

public class PlanFundingSummary
{
    public int PlanId { get; set; }
    public decimal LoanAmount { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = null!;
    public decimal AmountAllocated { get; set; }
    public decimal? FundingSharePercent { get; set; }
}

public class ProfitByPeriod
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string PeriodName { get; set; } = null!;
    public decimal? TotalProfit { get; set; }
    public decimal? TotalCostRecovery { get; set; }
    public decimal? TotalCollected { get; set; }
    public int PaymentsReceived { get; set; }
}

public class CashLedgerByPeriod
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string PeriodName { get; set; } = null!;
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal NetChange { get; set; }
}

public class PendingInstallment
{
    public int PlanId { get; set; }
    public string CustomerName { get; set; } = null!;
    public int PaymentId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountOutstanding { get; set; }
    public DateTime DueDate { get; set; }
    public string? Status { get; set; }
}

public class GuarantorPlanCount
{
    public int GuarantorId { get; set; }
    public int CustomerId { get; set; }
    public string GuarantorName { get; set; } = null!;
    public int ActivePlans { get; set; }
    public int TotalPlans { get; set; }
}

public class CashInHand
{
    public decimal CashInHandAmount { get; set; }
}

// Backs vw_CustomerPayments -- one row per payment transaction (down
// payment or installment alike), with the cost/profit split recomputed
// per-line using the plan's frozen profit rate. See the migration script
// for why this is safe to compute on the fly rather than store.
public class CustomerPayment
{
    public int TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public int PlanId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public int? InstallmentNumber { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal CostRecoveryAmount { get; set; }
}
