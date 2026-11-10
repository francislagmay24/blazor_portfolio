namespace Blazor_Portfolio.Data;

/// <summary>
/// An application user that can sign in. Maps to the <c>Users</c> table.
/// </summary>
public class AppUser
{
    public int Id { get; set; }

    /// <summary>Sign-in name. Always stored lower-cased.</summary>
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash produced by <see cref="Security.PasswordHasher"/>.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginUtc { get; set; }
}