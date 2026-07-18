using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductCategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ProductCategoryResponseDto>>> List() =>
        await _db.ProductCategories
            .Select(c => new ProductCategoryResponseDto(c.CategoryId, c.CategoryName, c.Description))
            .ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductCategoryResponseDto>> Get(int id)
    {
        var c = await _db.ProductCategories.FindAsync(id);
        if (c is null) return NotFound(new { error = $"Category {id} not found." });
        return new ProductCategoryResponseDto(c.CategoryId, c.CategoryName, c.Description);
    }

    [HttpPost]
    public async Task<ActionResult<ProductCategoryResponseDto>> Create(CreateProductCategoryDto dto)
    {
        var category = new ProductCategory { CategoryName = dto.CategoryName, Description = dto.Description, CreatedAt = DateTime.UtcNow };
        _db.ProductCategories.Add(category);
        await _db.SaveChangesAsync();
        var result = new ProductCategoryResponseDto(category.CategoryId, category.CategoryName, category.Description);
        return CreatedAtAction(nameof(Get), new { id = category.CategoryId }, result);
    }
}
