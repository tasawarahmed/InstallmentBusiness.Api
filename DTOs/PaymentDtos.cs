namespace InstallmentBusiness.Api.DTOs;

// Amount may exceed one installment -- it will be allocated across as many
// upcoming pending installments (in order) as it covers. This is how both
// a normal payment and an "advance" payment are represented.
public record RecordPaymentDto(
    decimal Amount, DateTime TransactionDate, string? PaymentMethod,
    string? ReferenceNo, string? ReceivedBy);

public record PaymentTransactionResponseDto(
    int TransactionId, int PlanId, int? PaymentId, int? InstallmentNumber,
    decimal AmountReceived, DateTime TransactionDate, string? PaymentMethod, string? ReferenceNo);

public record RecordPaymentResultDto(
    decimal AmountReceived, List<PaymentTransactionResponseDto> Allocations,
    decimal RemainingUnallocated);
