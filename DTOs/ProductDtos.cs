namespace InstallmentBusiness.Api.DTOs;

public record CreateProductCategoryDto(string CategoryName, string? Description);
public record ProductCategoryResponseDto(int CategoryId, string CategoryName, string? Description);

public record CreateProductDto(
    string ProductName, string? Brand, string? Model, int? CategoryId,
    decimal CostPrice, decimal SalePrice, string? Description);

public record UpdateProductDto(
    string ProductName, string? Brand, string? Model, int? CategoryId,
    decimal CostPrice, decimal SalePrice, string? Status, string? Description);

public record ProductResponseDto(
    int ProductId, string ProductName, string? Brand, string? Model,
    int? CategoryId, string? CategoryName, decimal CostPrice, decimal SalePrice,
    string? Status, string? Description);
