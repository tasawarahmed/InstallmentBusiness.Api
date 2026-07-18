namespace InstallmentBusiness.Api.Models.Entities;

// One row per actual cash receipt. PaymentId is nullable to allow an
// unallocated/advance receipt, though PaymentService always allocates
// incoming money to specific installments where possible (see
// PaymentService.RecordPaymentAsync).
public class PaymentTransaction
{
    public int TransactionId { get; set; }
    public int PlanId { get; set; }
    public int? PaymentId { get; set; }
    public decimal AmountReceived { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNo { get; set; }
    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }

    public InstallmentPlan Plan { get; set; } = null!;
    public InstallmentPayment? Installment { get; set; }
}
