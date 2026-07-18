namespace InstallmentBusiness.Api.Models.Entities;

public class Customer
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string CNIC { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Occupation { get; set; }
    public string? EmployerName { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InstallmentPlan> Plans { get; set; } = new List<InstallmentPlan>();
    public ICollection<Guarantor> GuarantorProfiles { get; set; } = new List<Guarantor>();
}
