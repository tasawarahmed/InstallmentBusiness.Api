namespace InstallmentBusiness.Api.Models.Entities;

// Status lifecycle: Proposed -> Active (requires >=1 guarantor, enforced
// by a DB trigger as well as PlanService) -> Completed / Defaulted / Cancelled.
public class InstallmentPlan
{
    public int PlanId { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }

    // Frozen at proposal time -- never re-read from Products afterward.
    public decimal ProductSalePrice { get; set; }
    public decimal ProductCostPrice { get; set; }

    public decimal DownPayment { get; set; }
    public decimal LoanAmount { get; set; }
    public int TenureMonths { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal TotalPayable { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Status { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<PlanGuarantor> PlanGuarantors { get; set; } = new List<PlanGuarantor>();
    public ICollection<InstallmentPayment> Installments { get; set; } = new List<InstallmentPayment>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<PlanFunding> PlanFundings { get; set; } = new List<PlanFunding>();

    // Total the customer ultimately pays across the life of the plan,
    // used as the basis for the profit-rate calculation.
    public decimal GrandTotal => DownPayment + TotalPayable;
}
