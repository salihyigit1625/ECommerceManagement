using ECommerceManagement.Domain.Entities;

namespace ECommerceManagement.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // --- Navigation Properties (1-to-1 İlişkiler) ---
    // Bir User'ın Customer profili olabilir, Seller profili olabilir veya hiçbiri olmayıp sadece Admin olabilir.
    public Customer? Customer { get; set; }
    public Seller? Seller { get; set; }

    // --- Navigation Properties (1-to-Many İlişkiler) ---
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}