namespace InstallmentBusiness.Api.DTOs;

public record CreateCustomerDto(
    string FirstName, string LastName, string CNIC, string Phone,
    string? AlternatePhone, string? Email, string? Address, string? City,
    DateTime? DateOfBirth, string? Occupation, string? EmployerName, decimal? MonthlyIncome);

public record UpdateCustomerDto(
    string FirstName, string LastName, string Phone, string? AlternatePhone,
    string? Email, string? Address, string? City, string? Occupation,
    string? EmployerName, decimal? MonthlyIncome, string? Status, string? Notes);

public record CustomerResponseDto(
    int CustomerId, string FirstName, string LastName, string CNIC, string Phone,
    string? Email, string? City, string? Status, decimal? MonthlyIncome);
