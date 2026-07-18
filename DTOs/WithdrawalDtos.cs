namespace InstallmentBusiness.Api.DTOs;

public record CreateWithdrawalDto(
    int InvestmentId, decimal Amount, DateTime WithdrawalDate,
    string? Status, string? Notes);

public record WithdrawalResponseDto(
    int WithdrawalId, int InvestmentId, decimal Amount,
    DateTime WithdrawalDate, string? Status, string? Notes);
