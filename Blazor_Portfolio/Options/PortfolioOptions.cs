namespace Blazor_Portfolio.Options;

/// <summary>
/// Every piece of copy on the portfolio landing page, bound from the
/// <c>Portfolio</c> configuration section (see <c>portfolio.json</c>).
/// Edit that file to change the page — no code changes or rebuild of markup needed.
/// </summary>
public sealed class PortfolioOptions
{
    public const string SectionName = "Portfolio";

    public ProfileInfo Profile { get; set; } = new();
    public AboutInfo About { get; set; } = new();
    public List<SkillGroup> Skills { get; set; } = [];
    public List<ProjectItem> Projects { get; set; } = [];
    public List<CredibilityItem> SocialProof { get; set; } = [];
    public ContactInfo Contact { get; set; } = new();
    public List<SocialLink> Socials { get; set; } = [];
}

public sealed class ProfileInfo
{
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string ValueProposition { get; set; } = "";
    public string Location { get; set; } = "";
    public string? AvailabilityNote { get; set; }
    public List<CallToAction> CallsToAction { get; set; } = [];
}

public sealed class CallToAction
{
    public string Label { get; set; } = "";
    public string Href { get; set; } = "";
    /// <summary>"primary" or "secondary".</summary>
    public string Style { get; set; } = "primary";
}

public sealed class AboutInfo
{
    public string Heading { get; set; } = "About me";
    public List<string> Paragraphs { get; set; } = [];
    public string AvatarPath { get; set; } = "images/portfolio/avatar-placeholder.svg";
    public string ResumePath { get; set; } = "files/resume.pdf";
    public string ResumeLabel { get; set; } = "Download résumé (PDF)";
    public List<FactItem> Facts { get; set; } = [];
}

public sealed class FactItem
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed class SkillGroup
{
    public string Category { get; set; } = "";
    public List<SkillItem> Items { get; set; } = [];
}

public sealed class SkillItem
{
    public string Name { get; set; } = "";
    /// <summary>Self-assessed level 1–5, rendered as a meter.</summary>
    public int Level { get; set; } = 3;
    public string? Years { get; set; }
}

public sealed class ProjectItem
{
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    /// <summary>1–3 bullets describing the hard parts and how you solved them.</summary>
    public List<string> Challenges { get; set; } = [];
    public List<string> Tech { get; set; } = [];
    public string ImagePath { get; set; } = "images/portfolio/project-placeholder.svg";
    public string? ImageAlt { get; set; }
    public string? DemoUrl { get; set; }
    public string? RepoUrl { get; set; }
    public string? Period { get; set; }
    public bool Featured { get; set; }
}

public sealed class CredibilityItem
{
    /// <summary>e.g. "Testimonial", "Certification", "Open source", "Award".</summary>
    public string Kind { get; set; } = "Testimonial";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    /// <summary>Who said it / who issued it. Optional.</summary>
    public string? Attribution { get; set; }
    public string? Url { get; set; }
    public string? Date { get; set; }
}

public sealed class ContactInfo
{
    public string Heading { get; set; } = "Get in touch";
    public string Blurb { get; set; } = "";
    public string Email { get; set; } = "";
    public string FormHeading { get; set; } = "Send a message";
}

public sealed class SocialLink
{
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
    /// <summary>Short text mark shown in the link, e.g. "GH", "in", "X".</summary>
    public string Mark { get; set; } = "";
}