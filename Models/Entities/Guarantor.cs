namespace InstallmentBusiness.Api.Models.Entities;

// A Guarantor is itself backed by a Customer record (the person vouching
// is also a customer in the system), and can back MULTIPLE plans via
// PlanGuarantors.
public class Guarantor
{
    public int GuarantorId { get; set; }
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string CNIC { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Relation { get; set; }
    public string? Address { get; set; }
    public string? Occupation { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<PlanGuarantor> PlanGuarantors { get; set; } = new List<PlanGuarantor>();
}
