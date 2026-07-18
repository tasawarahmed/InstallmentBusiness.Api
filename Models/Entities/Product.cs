namespace InstallmentBusiness.Api.Models.Entities;

public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? CategoryId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }

    public ProductCategory? Category { get; set; }
    public ICollection<InstallmentPlan> Plans { get; set; } = new List<InstallmentPlan>();
}
