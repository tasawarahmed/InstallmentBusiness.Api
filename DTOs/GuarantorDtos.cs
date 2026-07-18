namespace InstallmentBusiness.Api.DTOs;

public record CreateGuarantorDto(
    int CustomerId, string FirstName, string LastName, string CNIC, string Phone,
    string? Relation, string? Address, string? Occupation, decimal? MonthlyIncome);

public record GuarantorResponseDto(
    int GuarantorId, int CustomerId, string FirstName, string LastName,
    string CNIC, string Phone, string? Relation);
