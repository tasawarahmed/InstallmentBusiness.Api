namespace InstallmentBusiness.Api.DTOs;

public record CashLedgerEntryDto(
    int LedgerId,
    DateTime TransactionDate,
    string TransactionType,
    string Direction,
    decimal Amount,
    string? ReferenceTable,
    int? ReferenceId,
    string? Notes,
    DateTime? CreatedAt);

public record PagedCashLedgerResultDto(
    List<CashLedgerEntryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

// One line per payment (down payment or installment alike). TotalCostRecovery/
// TotalProfit/TotalPayments sum across the ENTIRE filtered date range, not
// just the current page -- render these as the totals row under the table
// regardless of which page is currently displayed.
public record CustomerPaymentLineDto(
    int TransactionId,
    DateTime TransactionDate,
    int PlanId,
    string CustomerName,
    int? InstallmentNumber,
    decimal CostRecoveryAmount,
    decimal ProfitAmount,
    decimal TotalPayment);

public record CustomerPaymentsReportDto(
    List<CustomerPaymentLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    decimal TotalCostRecovery,
    decimal TotalProfit,
    decimal TotalPayments);
