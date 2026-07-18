namespace InstallmentBusiness.Api.Models.Entities;

// Inserting with Status='Paid' fires the Insert cash-out trigger;
// inserting 'Pending' then updating to 'Paid' fires the Update trigger
// instead. Either path lands exactly one CashLedger row -- see
// InvestmentService for both flows.
public class ProfitPayment
{
    public int ProfitPaymentId { get; set; }
    public int InvestmentId { get; set; }
    public decimal ProfitAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Investment Investment { get; set; } = null!;
}
