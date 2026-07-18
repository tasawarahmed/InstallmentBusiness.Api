namespace InstallmentBusiness.Api.Models.Entities;

// Links investor capital to the loan(s) it funds. A plan may be funded
// by multiple investments; one investment may fund multiple plans.
public class PlanFunding
{
    public int PlanFundingId { get; set; }
    public int PlanId { get; set; }
    public int InvestmentId { get; set; }
    public decimal AmountAllocated { get; set; }
    public DateTime? CreatedAt { get; set; }

    public InstallmentPlan Plan { get; set; } = null!;
    public Investment Investment { get; set; } = null!;
}
