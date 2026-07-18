using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    private static readonly Func<Product, ProductResponseDto> ToDto = p => new ProductResponseDto(
        p.ProductId, p.ProductName, p.Brand, p.Model, p.CategoryId,
        p.Category != null ? p.Category.CategoryName : null,
        p.CostPrice, p.SalePrice, p.Status, p.Description);

    [HttpGet]
    public async Task<ActionResult<List<ProductResponseDto>>> List([FromQuery] int? categoryId)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        var products = await query.ToListAsync();
        return products.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> Get(int id)
    {
        var p = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.ProductId == id);
        if (p is null) return NotFound(new { error = $"Product {id} not found." });
        return ToDto(p);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> Create(CreateProductDto dto)
    {
        var product = new Product
        {
            ProductName = dto.ProductName, Brand = dto.Brand, Model = dto.Model,
            CategoryId = dto.CategoryId, CostPrice = dto.CostPrice, SalePrice = dto.SalePrice,
            Description = dto.Description, Status = "Available", CreatedAt = DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        await _db.Entry(product).Reference(p => p.Category).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = product.ProductId }, ToDto(product));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> Update(int id, UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound(new { error = $"Product {id} not found." });

        product.ProductName = dto.ProductName;
        product.Brand = dto.Brand;
        product.Model = dto.Model;
        product.CategoryId = dto.CategoryId;
        product.CostPrice = dto.CostPrice;
        product.SalePrice = dto.SalePrice;
        product.Status = dto.Status;
        product.Description = dto.Description;
        await _db.SaveChangesAsync();

        await _db.Entry(product).Reference(p => p.Category).LoadAsync();
        return ToDto(product);
    }
}
