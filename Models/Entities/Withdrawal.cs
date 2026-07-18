namespace InstallmentBusiness.Api.Models.Entities;

public class Withdrawal
{
    public int WithdrawalId { get; set; }
    public int InvestmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime WithdrawalDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Investment Investment { get; set; } = null!;
}
