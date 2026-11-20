namespace Blazor_Portfolio.Options;

using MailKit.Security;

/// <summary>
/// SMTP settings for delivering contact-form messages, bound from the
/// <c>Email</c> configuration section.
/// Leave <see cref="Host"/> empty to run without a mail server: submissions are
/// then stored in the database only and the failure is logged.
/// Keep the password in user-secrets, never in appsettings.json.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP host, e.g. <c>smtp.gmail.com</c>. Empty disables email delivery.</summary>
    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    /// <summary>
    /// TLS mode: <c>StartTls</c> (587), <c>SslOnConnect</c> (465), <c>Auto</c>,
    /// or <c>None</c> (plain — local sinks and test servers only).
    /// </summary>
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string UserName { get; set; } = "";

    /// <summary>SMTP password / app password. Supply via user-secrets.</summary>
    public string Password { get; set; } = "";

    /// <summary>Sender address. Must be the account you authenticate with (Gmail rejects anything else).</summary>
    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "";

    /// <summary>Inbox that receives the submissions — your personal address.</summary>
    public string ToAddress { get; set; } = "";

    /// <summary>Optional Cc, e.g. so a copy also lands in a second inbox.</summary>
    public string? CcAddress { get; set; }

    /// <summary>
    /// Set to false to stop sending both the notification and the visitor's
    /// auto-reply, without deleting the SMTP credentials.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Send the visitor a short "message received" confirmation.</summary>
    public bool SendAutoReply { get; set; } = true;

    /// <summary>
    /// True when there is enough configuration to attempt delivery. Used so a
    /// half-configured machine fails loudly in the log instead of per request.
    /// </summary>
    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(FromAddress)
        && !string.IsNullOrWhiteSpace(ToAddress)
        // A username with no password can only fail at AUTH, and Gmail then drops
        // the connection — treat it as "not configured" so the log is actionable.
        && (string.IsNullOrWhiteSpace(UserName) || !string.IsNullOrWhiteSpace(Password));

    /// <summary>
    /// Names the settings that are missing, so the log says "no password" instead
    /// of a bare <c>SmtpProtocolException</c> when Gmail rejects an empty login.
    /// </summary>
    public IReadOnlyList<string> MissingSettings()
    {
        if (!Enabled)
        {
            return ["Email:Enabled is false"];
        }

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Host))
        {
            missing.Add("Email:Host");
        }

        if (string.IsNullOrWhiteSpace(FromAddress))
        {
            missing.Add("Email:FromAddress");
        }

        if (string.IsNullOrWhiteSpace(ToAddress))
        {
            missing.Add("Email:ToAddress");
        }

        // Gmail requires a login; an empty password is the usual reason a send
        // fails, so report it as a configuration problem rather than a network one.
        if (!string.IsNullOrWhiteSpace(UserName) && string.IsNullOrWhiteSpace(Password))
        {
            missing.Add("Email:Password");
        }

        return missing;
    }
}