using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuarantorsController : ControllerBase
{
    private readonly AppDbContext _db;
    public GuarantorsController(AppDbContext db) => _db = db;

    private static readonly Func<Guarantor, GuarantorResponseDto> ToDto = g => new GuarantorResponseDto(
        g.GuarantorId, g.CustomerId, g.FirstName, g.LastName, g.CNIC, g.Phone, g.Relation);

    [HttpGet]
    public async Task<ActionResult<List<GuarantorResponseDto>>> List()
    {
        var guarantors = await _db.Guarantors.ToListAsync();
        return guarantors.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GuarantorResponseDto>> Get(int id)
    {
        var g = await _db.Guarantors.FindAsync(id);
        if (g is null) return NotFound(new { error = $"Guarantor {id} not found." });
        return ToDto(g);
    }

    [HttpPost]
    public async Task<ActionResult<GuarantorResponseDto>> Create(CreateGuarantorDto dto)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.CustomerId == dto.CustomerId);
        if (!customerExists) return NotFound(new { error = $"Customer {dto.CustomerId} not found." });

        var guarantor = new Guarantor
        {
            CustomerId = dto.CustomerId, FirstName = dto.FirstName, LastName = dto.LastName,
            CNIC = dto.CNIC, Phone = dto.Phone, Relation = dto.Relation, Address = dto.Address,
            Occupation = dto.Occupation, MonthlyIncome = dto.MonthlyIncome, CreatedAt = DateTime.UtcNow
        };
        _db.Guarantors.Add(guarantor);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = guarantor.GuarantorId }, ToDto(guarantor));
    }
}
