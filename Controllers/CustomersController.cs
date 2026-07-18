using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersController(AppDbContext db) => _db = db;

    private static readonly Func<Customer, CustomerResponseDto> ToDto = c => new CustomerResponseDto(
        c.CustomerId, c.FirstName, c.LastName, c.CNIC, c.Phone, c.Email, c.City, c.Status, c.MonthlyIncome);

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponseDto>>> List([FromQuery] string? search)
    {
        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.FirstName.Contains(search) || c.LastName.Contains(search) || c.CNIC.Contains(search));
        var customers = await query.ToListAsync();
        return customers.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponseDto>> Get(int id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c is null) return NotFound(new { error = $"Customer {id} not found." });
        return ToDto(c);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponseDto>> Create(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            FirstName = dto.FirstName, LastName = dto.LastName, CNIC = dto.CNIC, Phone = dto.Phone,
            AlternatePhone = dto.AlternatePhone, Email = dto.Email, Address = dto.Address, City = dto.City,
            DateOfBirth = dto.DateOfBirth, Occupation = dto.Occupation, EmployerName = dto.EmployerName,
            MonthlyIncome = dto.MonthlyIncome, Status = "Active",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = customer.CustomerId }, ToDto(customer));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerResponseDto>> Update(int id, UpdateCustomerDto dto)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound(new { error = $"Customer {id} not found." });

        customer.FirstName = dto.FirstName;
        customer.LastName = dto.LastName;
        customer.Phone = dto.Phone;
        customer.AlternatePhone = dto.AlternatePhone;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.City = dto.City;
        customer.Occupation = dto.Occupation;
        customer.EmployerName = dto.EmployerName;
        customer.MonthlyIncome = dto.MonthlyIncome;
        customer.Status = dto.Status;
        customer.Notes = dto.Notes;
        customer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(customer);
    }
}
