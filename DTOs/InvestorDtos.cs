namespace InstallmentBusiness.Api.DTOs;

public record CreateInvestorDto(
    string FirstName, string LastName, string CNIC, string? Phone,
    string? Email, string? Address, decimal? DefaultProfitRate);

public record InvestorResponseDto(
    int InvestorId, string FirstName, string LastName, string CNIC,
    string? Phone, string? Email, decimal? DefaultProfitRate);
