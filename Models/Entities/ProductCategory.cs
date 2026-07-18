namespace InstallmentBusiness.Api.Models.Entities;

public class ProductCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
