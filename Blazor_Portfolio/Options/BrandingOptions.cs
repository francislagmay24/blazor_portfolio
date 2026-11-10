namespace Blazor_Portfolio.Options;

/// <summary>
/// White-label branding settings, read from the <c>Branding</c> configuration section.
/// Swap <see cref="LogoPath"/> (and the colours in <c>Login.razor.css</c>) per customer.
/// </summary>
public sealed class BrandingOptions
{
    public const string SectionName = "Branding";

    public string ProductName { get; set; } = "Blazor Portfolio";

    public string Tagline { get; set; } = "Sign in to continue to your workspace.";

    /// <summary>
    /// Path to the brand image shown on the login page. Defaults to the bundled
    /// placeholder; point it at a customer logo to white-label the screen.
    /// </summary>
    public string LogoPath { get; set; } = "images/whitelabel-placeholder.svg";

    /// <summary>Set to render a text brand mark instead of an image.</summary>
    public bool UseImageLogo { get; set; } = true;
}