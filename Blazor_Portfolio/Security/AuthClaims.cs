using System.Security.Claims;

namespace Blazor_Portfolio.Security;

/// <summary>Shared claims-building helpers for the app cookie.</summary>
public static class AuthClaims
{
    public static ClaimsPrincipal BuildPrincipal(AppUserView user) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim("display_name", user.DisplayName)
        ], UserService.CookieScheme));

    /// <summary>The fields the UI needs; deliberately excludes the password hash.</summary>
    public sealed record AppUserView(int Id, string Email, string DisplayName);
}