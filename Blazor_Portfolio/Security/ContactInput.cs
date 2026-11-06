using System.ComponentModel.DataAnnotations;

namespace Blazor_Portfolio.Security;

/// <summary>Form model posted by the portfolio contact form.</summary>
public sealed class ContactInput
{
    [Required(ErrorMessage = "Please tell me your name.")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "An email address is required so I can reply.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? Company { get; set; }

    [Required(ErrorMessage = "Please write a message.")]
    [MinLength(10, ErrorMessage = "A little more detail would help — at least 10 characters.")]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Honeypot. Hidden from users, so any value here means a bot filled it in.
    /// Checked by <see cref="Blazor_Portfolio.Services.ContactService"/>.
    /// </summary>
    public string? Website { get; set; }
}