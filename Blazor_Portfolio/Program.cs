using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Blazor_Portfolio.Components;
using Blazor_Portfolio.Data;
using Blazor_Portfolio.Options;
using Blazor_Portfolio.Security;
using Blazor_Portfolio.Services;

// Utility: print a PasswordHasher-compatible hash and exit, e.g.
//   dotnet run -- --hash-password "MyNewPassword"
// Use the output as the PasswordHash value in Database/schema.sql.
if (args is ["--hash-password", var passwordToHash])
{
    Console.WriteLine(PasswordHasher.Hash(passwordToHash));
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Portfolio copy lives in its own file so the page can be edited without touching code.
builder.Configuration.AddJsonFile("portfolio.json", optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.Configure<PortfolioOptions>(builder.Configuration.GetSection(PortfolioOptions.SectionName));

// --- Database (MySQL via Pomelo) ---------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is missing. Add it to appsettings.json or user-secrets.");

// The version is pinned in configuration rather than auto-detected so the app can
// start (and show a friendly login error) while MySQL is unreachable.
var serverVersion = new MySqlServerVersion(
    Version.Parse(builder.Configuration["Database:ServerVersion"] ?? "8.0.36"));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ContactService>();

// --- Authentication / authorization ------------------------------------------
builder.Services.AddAuthentication(UserService.CookieScheme)
    .AddCookie(UserService.CookieScheme, options =>
    {
        options.Cookie.Name = "BlazorPortfolio.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Clears the auth cookie and returns to the login page. Posted to (not linked) so a
// bare GET cannot sign the user out; the token is validated explicitly because
// minimal APIs only auto-validate antiforgery for form-binding endpoints.
app.MapPost("/auth/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("The sign-out request could not be verified. Reload the page and try again.");
    }

    await context.SignOutAsync(UserService.CookieScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();