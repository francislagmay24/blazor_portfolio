using Microsoft.EntityFrameworkCore;
using Blazor_Portfolio.Data;
using Blazor_Portfolio.Security;

namespace Blazor_Portfolio.Services;

public enum ContactResult
{
    Saved,
    /// <summary>Honeypot filled in — treated as success by the UI so bots learn nothing.</summary>
    Discarded,
    ServiceUnavailable
}

/// <summary>Persists portfolio contact form submissions.</summary>
public sealed class ContactService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ContactService> logger)
{
    public async Task<ContactResult> SaveAsync(
        ContactInput input,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(input.Website))
        {
            logger.LogInformation("Discarded a contact message that filled the honeypot field.");
            return ContactResult.Discarded;
        }

        var message = new ContactMessage
        {
            Name = input.Name.Trim(),
            Email = input.Email.Trim().ToLowerInvariant(),
            Company = string.IsNullOrWhiteSpace(input.Company) ? null : input.Company.Trim(),
            Message = input.Message.Trim(),
            CreatedUtc = DateTime.UtcNow,
            IsHandled = false,
            IpAddress = ipAddress
        };

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            db.ContactMessages.Add(message);
            await db.SaveChangesAsync(cancellationToken);
            return ContactResult.Saved;
        }
        catch (Exception ex)
        {
            // Losing a lead silently is worse than a retry prompt, so tell the user.
            logger.LogError(ex, "Could not save a contact message from {Email}.", message.Email);
            return ContactResult.ServiceUnavailable;
        }
    }
}