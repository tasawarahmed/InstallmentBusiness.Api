using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IInvestmentService _investments;
    public InvestmentsController(AppDbContext db, IInvestmentService investments) { _db = db; _investments = investments; }

    private static InvestmentResponseDto ToDto(Models.Entities.Investment i) => new(
        i.InvestmentId, i.InvestorId, i.Investor?.FirstName + " " + i.Investor?.LastName,
        i.Amount, i.InvestmentDate, i.ProfitRate, i.Status, i.MaturityDate);

    [HttpGet]
    public async Task<ActionResult<List<InvestmentResponseDto>>> List([FromQuery] int? investorId)
    {
        var query = _db.Investments.Include(i => i.Investor).AsQueryable();
        if (investorId.HasValue) query = query.Where(i => i.InvestorId == investorId);
        var investments = await query.ToListAsync();
        return investments.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvestmentResponseDto>> Get(int id)
    {
        var i = await _db.Investments.Include(x => x.Investor).FirstOrDefaultAsync(x => x.InvestmentId == id);
        if (i is null) return NotFound(new { error = $"Investment {id} not found." });
        return ToDto(i);
    }

    // Fires trg_Investments_CashIn -- CashLedger reflects this immediately.
    [HttpPost]
    public async Task<ActionResult<InvestmentResponseDto>> Create(CreateInvestmentDto dto)
    {
        var investment = await _investments.RecordInvestmentAsync(dto);
        await _db.Entry(investment).Reference(i => i.Investor).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = investment.InvestmentId }, ToDto(investment));
    }

    // Records which plan(s) this investment's capital funds. Independent of
    // the cash-in event above -- an investment can exist unallocated.
    [HttpPost("funding")]
    public async Task<IActionResult> AllocateFunding(AllocateFundingDto dto)
    {
        var funding = await _investments.AllocateFundingAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = funding.InvestmentId },
            new { funding.PlanFundingId, funding.PlanId, funding.InvestmentId, funding.AmountAllocated });
    }
}
