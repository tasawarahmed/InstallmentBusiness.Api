using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ExpensesController(AppDbContext db) => _db = db;

    private static ExpenseResponseDto ToDto(Expense e) => new(
        e.ExpenseId, e.Category, e.Amount, e.ExpenseDate, e.Description, e.PaidTo, e.PaymentMethod, e.ReferenceNo);

    // Paginated for the same reason /api/reports/cash-ledger is: this table
    // grows by one row every time an expense is recorded, for the life of
    // the business, with no natural ceiling. The DTO is constructed inline
    // inside the query (not via ToDto) so Skip/Take run as a real
    // server-side OFFSET/FETCH rather than materializing the whole table
    // first -- same reasoning as the cash-ledger endpoint.
    [HttpGet]
    public async Task<ActionResult<PagedExpenseResultDto>> List(
        [FromQuery] string? category,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 500) pageSize = 50;

        var query = _db.Expenses.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);
        if (startDate.HasValue) query = query.Where(e => e.ExpenseDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(e => e.ExpenseDate <= endDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.ExpenseId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExpenseResponseDto(
                e.ExpenseId, e.Category, e.Amount, e.ExpenseDate, e.Description, e.PaidTo, e.PaymentMethod, e.ReferenceNo))
            .ToListAsync();

        return new PagedExpenseResultDto(items, totalCount, page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseResponseDto>> Get(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return NotFound(new { error = $"Expense {id} not found." });
        return ToDto(expense);
    }

    // Fires trg_Expenses_CashOut -- CashLedger reflects this immediately.
    [HttpPost]
    public async Task<ActionResult<ExpenseResponseDto>> Create(CreateExpenseDto dto)
    {
        if (dto.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(dto.Category))
            throw new ArgumentException("Category is required.");

        var expense = new Expense
        {
            Category = dto.Category,
            Amount = dto.Amount,
            ExpenseDate = dto.ExpenseDate,
            Description = dto.Description,
            PaidTo = dto.PaidTo,
            PaymentMethod = dto.PaymentMethod,
            ReferenceNo = dto.ReferenceNo,
            CreatedAt = DateTime.UtcNow
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = expense.ExpenseId }, ToDto(expense));
    }
}
