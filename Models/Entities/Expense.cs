namespace InstallmentBusiness.Api.Models.Entities;

// General operating expenses (rent, salaries, utilities, etc.) -- NOT tied
// to a specific product purchase. Inserting a row here fires
// trg_Expenses_CashOut, which is the only thing that writes to CashLedger
// on this table's behalf -- the API never inserts into CashLedger directly.
public class Expense
{
    public int ExpenseId { get; set; }
    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? PaidTo { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime? CreatedAt { get; set; }
}