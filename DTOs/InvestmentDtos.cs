namespace InstallmentBusiness.Api.DTOs;

public record CreateInvestmentDto(
    int InvestorId, decimal Amount, DateTime InvestmentDate,
    decimal? ProfitRate, DateTime? MaturityDate);

public record InvestmentResponseDto(
    int InvestmentId, int InvestorId, string InvestorName, decimal Amount,
    DateTime InvestmentDate, decimal? ProfitRate, string? Status, DateTime? MaturityDate);

public record AllocateFundingDto(int PlanId, int InvestmentId, decimal AmountAllocated);
