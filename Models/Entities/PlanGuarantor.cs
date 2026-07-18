namespace InstallmentBusiness.Api.Models.Entities;

// Junction table: a plan requires one-or-more guarantors, and the same
// guarantor may back multiple plans.
public class PlanGuarantor
{
    public int PlanGuarantorId { get; set; }
    public int PlanId { get; set; }
    public int GuarantorId { get; set; }
    public DateTime? CreatedAt { get; set; }

    public InstallmentPlan Plan { get; set; } = null!;
    public Guarantor Guarantor { get; set; } = null!;
}
