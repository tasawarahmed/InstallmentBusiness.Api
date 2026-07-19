namespace InstallmentBusiness.Api.DTOs;

public record CreateExpenseDto(
    string Category,
    decimal Amount,
    DateTime ExpenseDate,
    string? Description,
    string? PaidTo,
    string? PaymentMethod,
    string? ReferenceNo);

public record ExpenseResponseDto(
    int ExpenseId,
    string Category,
    decimal Amount,
    DateTime ExpenseDate,
    string? Description,
    string? PaidTo,
    string? PaymentMethod,
    string? ReferenceNo);

public record PagedExpenseResultDto(
    List<ExpenseResponseDto> Items,
    int TotalCount,
    int Page,
    int PageSize);