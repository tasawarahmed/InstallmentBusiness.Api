namespace InstallmentBusiness.Api.Models.Entities;

// Inserting a row here fires trg_Investments_CashIn (+CashLedger).
// Amount is never decremented directly -- withdrawals are tracked
// separately in Withdrawals and netted off in vw_InvestorLedger, so the
// historical "how much was originally invested" figure is preserved.
public class Investment
{
    public int InvestmentId { get; set; }
    public int InvestorId { get; set; }
    public decimal Amount { get; set; }
    public DateTime InvestmentDate { get; set; }
    public decimal? ProfitRate { get; set; }
    public string? Status { get; set; }
    public DateTime? MaturityDate { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Investor Investor { get; set; } = null!;
    public ICollection<ProfitPayment> ProfitPayments { get; set; } = new List<ProfitPayment>();
    public ICollection<Withdrawal> Withdrawals { get; set; } = new List<Withdrawal>();
    public ICollection<PlanFunding> PlanFundings { get; set; } = new List<PlanFunding>();
}
