namespace InstallmentBusiness.Api.Models.Entities;

// Populated ENTIRELY by DB triggers (PaymentTransactions, Investments,
// ProfitPayments, Withdrawals). The API only ever reads this table --
// never inserts into it directly -- so there is exactly one place cash
// movements can originate from.
public class CashLedger
{
    public int LedgerId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = null!;
    public string Direction { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? ReferenceTable { get; set; }
    public int? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
}
