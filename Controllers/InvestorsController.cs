using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestorsController : ControllerBase
{
    private readonly AppDbContext _db;
    public InvestorsController(AppDbContext db) => _db = db;

    private static readonly Func<Investor, InvestorResponseDto> ToDto = i => new InvestorResponseDto(
        i.InvestorId, i.FirstName, i.LastName, i.CNIC, i.Phone, i.Email, i.DefaultProfitRate);

    [HttpGet]
    public async Task<ActionResult<List<InvestorResponseDto>>> List()
    {
        var investors = await _db.Investors.ToListAsync();
        return investors.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvestorResponseDto>> Get(int id)
    {
        var i = await _db.Investors.FindAsync(id);
        if (i is null) return NotFound(new { error = $"Investor {id} not found." });
        return ToDto(i);
    }

    [HttpPost]
    public async Task<ActionResult<InvestorResponseDto>> Create(CreateInvestorDto dto)
    {
        var investor = new Investor
        {
            FirstName = dto.FirstName, LastName = dto.LastName, CNIC = dto.CNIC,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address,
            DefaultProfitRate = dto.DefaultProfitRate, CreatedAt = DateTime.UtcNow
        };
        _db.Investors.Add(investor);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = investor.InvestorId }, ToDto(investor));
    }
}
