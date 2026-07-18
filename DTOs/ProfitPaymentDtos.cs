namespace InstallmentBusiness.Api.DTOs;

// Status defaults to "Paid" (immediate) if not specified; pass "Pending"
// to record it first and mark it paid later via the MarkPaid endpoint --
// both paths correctly reach CashLedger exactly once (see InvestmentService).
public record CreateProfitPaymentDto(
    int InvestmentId, decimal ProfitAmount, DateTime PaymentDate,
    string? PaymentMethod, string? Status);

public record ProfitPaymentResponseDto(
    int ProfitPaymentId, int InvestmentId, decimal ProfitAmount,
    DateTime PaymentDate, string? PaymentMethod, string? Status);

public record MarkProfitPaymentPaidDto(string? PaymentMethod);
