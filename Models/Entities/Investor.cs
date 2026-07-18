namespace InstallmentBusiness.Api.Models.Entities;

public class Investor
{
    public int InvestorId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string CNIC { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal? DefaultProfitRate { get; set; }
    public DateTime? CreatedAt { get; set; }

    public ICollection<Investment> Investments { get; set; } = new List<Investment>();
}
