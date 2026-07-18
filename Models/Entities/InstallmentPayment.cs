namespace InstallmentBusiness.Api.Models.Entities;

// One row per scheduled payment on a plan. InstallmentNumber = 0 represents
// the down payment; 1..TenureMonths represent the monthly installments.
// AmountPaid/Status/PaidDate are kept in sync by a DB trigger whenever a
// PaymentTransaction is inserted against this row -- the API never writes
// those three columns directly. CostRecoveryAmount/ProfitAmount ARE written
// by the API (ProfitCalculator), incrementally, at the same time.
public class InstallmentPayment
{
    public int PaymentId { get; set; }
    public int PlanId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal AmountDue { get; set; }
    public decimal? AmountPaid { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public decimal? PenaltyAmount { get; set; }
    public decimal? CostRecoveryAmount { get; set; }
    public decimal? ProfitAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Status { get; set; }
    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }

    public InstallmentPlan Plan { get; set; } = null!;
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();

    public decimal Outstanding => AmountDue - (AmountPaid ?? 0);
}
