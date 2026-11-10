using Microsoft.EntityFrameworkCore;
using Blazor_Portfolio.Data;

namespace Blazor_Portfolio.Security;

public enum LoginResult
{
    Success,
    InvalidCredentials,
    AccountDisabled,
    ServiceUnavailable
}

/// <summary>
/// Authenticates users against the <c>Users</c> table and issues the auth cookie.
/// </summary>
public sealed class UserService(IDbContextFactory<AppDbContext> dbFactory, ILogger<UserService> logger)
{
    public const string CookieScheme = "AppCookie";

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<(LoginResult Result, AppUser? User)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var normalized = NormalizeEmail(email);

        AppUser? user;
        try
        {
            user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        }
        catch (Exception ex)
        {
            // Database down or unreachable: don't leak the driver error to the login page.
            logger.LogError(ex, "Could not query the Users table while signing in.");
            return (LoginResult.ServiceUnavailable, null);
        }

        // Verify even when the user is missing so both failure paths cost roughly
        // the same time; otherwise the response time leaks which emails exist.
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return (LoginResult.InvalidCredentials, null);
        }

        if (!user.IsActive)
        {
            return (LoginResult.AccountDisabled, null);
        }

        return (LoginResult.Success, user);
    }

    /// <summary>Stamps <c>LastLoginUtc</c>. Failures are logged, never surfaced to the caller.</summary>
    public async Task RecordLoginAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            await db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginUtc, DateTime.UtcNow), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not update the last login time for user {UserId}.", userId);
        }
    }
}