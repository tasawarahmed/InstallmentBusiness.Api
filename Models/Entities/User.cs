namespace InstallmentBusiness.Api.Models.Entities;

// Deliberately minimal: one flat tier of authenticated users, no roles or
// permissions yet. Anyone who can log in can call anything -- this matches
// the "limited circulation" scope. If role-based access is needed later,
// add a Role/Permission concept on top of this rather than reworking it.
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}
