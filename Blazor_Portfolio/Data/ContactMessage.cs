namespace Blazor_Portfolio.Data;

/// <summary>
/// A message submitted through the portfolio contact form. Maps to <c>ContactMessages</c>.
/// </summary>
public class ContactMessage
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Company { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Set once the message has been read and answered.</summary>
    public bool IsHandled { get; set; }

    /// <summary>Best-effort client IP, for abuse triage. Null when unavailable.</summary>
    public string? IpAddress { get; set; }
}