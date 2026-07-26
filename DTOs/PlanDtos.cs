namespace InstallmentBusiness.Api.DTOs;

// ProductCostPrice/ProductSalePrice are NOT accepted here -- they are always
// snapshotted server-side from the Product at proposal time (frozen from then on).
public record CreatePlanProposalDto(
    int CustomerId, int ProductId, decimal DownPayment, int TenureMonths,
    decimal MonthlyInstallment, decimal TotalPayable, DateTime StartDate,
    string? ApprovedBy, string? Notes);

public record AddGuarantorToPlanDto(int GuarantorId);

// reason is optional but recommended -- it's appended to the installment's
// Notes as a lightweight audit trail (who/why isn't tracked beyond this).
public record RescheduleInstallmentDto(DateTime NewDueDate, string? Reason);

// Down payment is collected at the moment of finalization -- these let the
// caller record how it was actually received.
public record FinalizePlanDto(string? DownPaymentMethod, string? DownPaymentReferenceNo);

public record PlanResponseDto(
    int PlanId, int CustomerId, string CustomerName, int ProductId, string ProductName,
    decimal ProductSalePrice, decimal ProductCostPrice, decimal DownPayment,
    decimal LoanAmount, int TenureMonths, decimal MonthlyInstallment, decimal TotalPayable,
    DateTime StartDate, DateTime EndDate, string? Status, int GuarantorCount);

public record InstallmentScheduleItemDto(
    int PaymentId, int InstallmentNumber, decimal AmountDue, decimal AmountPaid,
    decimal Outstanding, DateTime DueDate, DateTime? PaidDate, string? Status,
    decimal CostRecoveryAmount, decimal ProfitAmount);
