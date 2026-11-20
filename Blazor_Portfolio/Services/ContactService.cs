using Microsoft.EntityFrameworkCore;
using Blazor_Portfolio.Data;
using Blazor_Portfolio.Security;

namespace Blazor_Portfolio.Services;

public enum ContactResult
{
    /// <summary>Stored and emailed.</summary>
    Delivered,
    /// <summary>Emailed, but the database write failed — the lead is still safe in the inbox.</summary>
    Emailed,
    /// <summary>Stored, but the email did not go out. Visible so a broken mail setup is not silent.</summary>
    StoredOnly,
    /// <summary>
    /// Stored, but email is not configured at all — nothing reached the owner's
    /// inbox. Kept distinct so the form does not claim the message was delivered.
    /// </summary>
    NotEmailed,
    /// <summary>Honeypot filled in — treated as success by the UI so bots learn nothing.</summary>
    Discarded,
    /// <summary>Neither channel worked.</summary>
    ServiceUnavailable
}

/// <summary>
/// Handles portfolio contact form submissions: emails them to the owner and
/// keeps a database record so a mail outage never loses a lead.
/// </summary>
public sealed class ContactService(
    IDbContextFactory<AppDbContext> dbFactory,
    ContactEmailService emailService,
    ILogger<ContactService> logger)
{
    public async Task<ContactResult> SubmitAsync(
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

        // Email first: it is the channel the owner actually reads, and the two
        // calls share one cancellation budget so a hung SMTP server cannot hold
        // the form open indefinitely.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));

        var emailResult = await emailService.SendAsync(input, ipAddress, timeout.Token);
        var stored = await TryStoreAsync(message, cancellationToken);

        return (emailResult, stored) switch
        {
            (EmailResult.Sent, true) => ContactResult.Delivered,
            (EmailResult.Sent, false) => ContactResult.Emailed,

            // An unconfigured mail server is not a delivery: the row is saved, but the
            // owner's inbox never sees it, so the sender must be told.
            (EmailResult.NotConfigured, true) => ContactResult.NotEmailed,
            (EmailResult.NotConfigured, false) => ContactResult.ServiceUnavailable,

            // Without email the visitor must still be told something went wrong,
            // otherwise the enquiry dies silently in a table nobody polls.
            (EmailResult.AuthenticationFailed, true) => ContactResult.StoredOnly,
            (EmailResult.AuthenticationFailed, false) => ContactResult.ServiceUnavailable,
            (EmailResult.Failed, true) => ContactResult.StoredOnly,
            (EmailResult.Failed, false) => ContactResult.ServiceUnavailable,
            _ => ContactResult.ServiceUnavailable
        };
    }

    private async Task<bool> TryStoreAsync(ContactMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            db.ContactMessages.Add(message);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not store a contact message from {Email}.", message.Email);
            return false;
        }
    }
}
