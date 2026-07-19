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