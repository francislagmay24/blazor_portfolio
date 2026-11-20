using Blazor_Portfolio.Options;
using Blazor_Portfolio.Security;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Blazor_Portfolio.Services;

public enum EmailResult
{
    Sent,
    /// <summary>No SMTP host/credentials configured — the caller still has the database row.</summary>
    NotConfigured,
    /// <summary>
    /// The server rejected the credentials (Gmail: HTTP 535 / "Username and
    /// Password not accepted"). Distinct from a network failure because the fix
    /// is an App Password, not a retry.
    /// </summary>
    AuthenticationFailed,
    Failed
}

/// <summary>Emails portfolio contact-form submissions to the site owner via SMTP (MailKit).</summary>
public sealed class ContactEmailService(
    IOptions<EmailOptions> options,
    ILogger<ContactEmailService> logger)
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(20);

    private const string DefaultFromName = "Portfolio contact form";

    private readonly EmailOptions _options = options.Value;

    private string FromName =>
        string.IsNullOrWhiteSpace(_options.FromName) ? DefaultFromName : _options.FromName.Trim();

    /// <summary>
    /// Flattens user-supplied text to one line. MimeKit already encodes CR/LF so a
    /// header cannot be injected, but a stacked name would otherwise produce an
    /// unreadable subject and a "Hi Evil Bcc: ..., thanks..." greeting.
    /// </summary>
    private static string SingleLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var flattened = new string(value.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
        return string.Join(' ', flattened.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Emails the submission to the owner and, when enabled, a receipt to the
    /// visitor. Never throws: a mail failure must not lose the form submission,
    /// which the caller has already persisted.
    /// </summary>
    public async Task<EmailResult> SendAsync(
        ContactInput input,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            // An empty password is the commonest cause and used to surface as an
            // opaque SmtpProtocolException, so name the gap explicitly.
            var missing = _options.MissingSettings();

            if (missing.Count > 0)
            {
                logger.LogError(
                    "Contact email skipped: {Missing} not configured. Set it with: " +
                    "dotnet user-secrets set \"{Key}\" \"<gmail app password>\". " +
                    "The message from {Email} was stored but not emailed.",
                    string.Join(", ", missing),
                    missing.Contains("Email:Password") ? "Email:Password" : missing[0],
                    input.Email);
            }

            return EmailResult.NotConfigured;
        }

        var notifications = new List<MimeMessage> { BuildNotification(input, ipAddress) };

        if (_options.SendAutoReply)
        {
            notifications.Add(BuildAutoReply(input));
        }

        var sent = 0;

        try
        {
            using var client = new SmtpClient { Timeout = (int)SendTimeout.TotalMilliseconds };

            await client.ConnectAsync(_options.Host, _options.Port, _options.Security, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
            }

            foreach (var message in notifications)
            {
                // MailKit 5 replaced SendAsync(MimeMessage) with a request object
                // that also reports per-recipient acceptance.
                var result = await client.SendAsync(new SendRequest(message), cancellationToken);

                if (result.RejectedRecipients.Count > 0)
                {
                    logger.LogWarning(
                        "The SMTP server rejected {Rejected} recipient(s) for the contact message from {Email}: {Response}",
                        result.RejectedRecipients.Count, input.Email, result.ResponseText);
                }

                sent++;
            }

            await client.DisconnectAsync(quit: true, cancellationToken);

            logger.LogInformation(
                "Emailed contact message from {Email} to {Recipient} ({Count} message(s)).",
                input.Email, _options.ToAddress, sent);
            return EmailResult.Sent;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Contact email was cancelled after sending {Count} message(s).", sent);
            return EmailResult.Failed;
        }
        // Gmail answers bad credentials with 535. MailKit surfaces that as an
        // AuthenticationException, but a rejected login can also drop the socket
        // and surface as SmtpProtocolException("unexpectedly disconnected"), so
        // both are treated as an authentication problem — retrying cannot help.
        catch (Exception ex) when (ex is MailKit.Security.AuthenticationException or SmtpProtocolException)
        {
            logger.LogError(
                "SMTP authentication failed for {User}: {Reason}. " +
                "Gmail requires a 16-character App Password (https://myaccount.google.com/apppasswords), " +
                "not the normal account password. The message from {Email} was stored in the database.",
                _options.UserName, ex.Message, input.Email);
            return EmailResult.AuthenticationFailed;
        }
        catch (Exception ex)
        {
            // Reaching here means the owner may not learn about the enquiry from
            // email alone, so log it loudly — the database row is the fallback.
            logger.LogError(ex,
                "Could not email contact message from {Email} (sent {Count}/{Total} message(s)). The message is stored in the database.",
                input.Email, sent, notifications.Count);
            return EmailResult.Failed;
        }
    }

    private MimeMessage BuildNotification(ContactInput input, string? ipAddress)
    {
        var name = SingleLine(input.Name);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(_options.ToAddress));

        if (!string.IsNullOrWhiteSpace(_options.CcAddress))
        {
            message.Cc.Add(MailboxAddress.Parse(_options.CcAddress));
        }

        // Replying in the mail client answers the visitor, not your own inbox.
        message.ReplyTo.Add(new MailboxAddress(name, input.Email));
        message.Subject = $"Portfolio enquiry from {name}";

        var company = SingleLine(input.Company);
        var sender = string.IsNullOrWhiteSpace(ipAddress) ? "not available" : ipAddress;

        // Also expose the IP as a header so it shows in the inbox list without
        // opening the message — handy for quick spam triage.
        message.Headers.Add("X-Sender-IP", sender);

        var body = $"""
            New message from the portfolio contact form.

            Name:    {name}
            Email:   {input.Email.Trim()}
            Company: {(company.Length == 0 ? "—" : company)}
            Sent:    {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            IP:      {sender}

            ------------------------------------------------------------

            Message: 
            {input.Message.Trim()}
            """;

        message.Body = new TextPart(TextFormat.Plain) { Text = body };
        return message;
    }

    private MimeMessage BuildAutoReply(ContactInput input)
    {
        var name = SingleLine(input.Name);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(name, input.Email));
        message.Subject = "Thanks for getting in touch";

        var body = $"""
            Hi {name},

            Thanks for reaching out through my portfolio — your message arrived and
            I'll get back to you, usually within two working days.

            For your records, this is what you sent:

            ------------------------------------------------------------

            {input.Message.Trim()}
            """;

        message.Body = new TextPart(TextFormat.Plain) { Text = body };
        return message;
    }
}