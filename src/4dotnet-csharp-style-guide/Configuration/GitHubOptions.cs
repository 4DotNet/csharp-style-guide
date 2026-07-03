namespace FourDotNet.CSharpStyleGuide.Configuration;

/// <summary>
/// Configuration for the GitHub repository that hosts the style-guide documents.
/// Bound from the "GitHub" configuration section.
/// </summary>
public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";

    /// <summary>
    /// Base address of the GitHub REST API. Defaults to public GitHub; override this
    /// to target a GitHub Enterprise Server instance (e.g. https://github.contoso.com/api/v3/).
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.github.com/";

    /// <summary>GitHub organization (or user) that owns the repository.</summary>
    public string Organization { get; set; } = "4Dotnet";

    /// <summary>Name of the repository that contains the <c>designs</c> folder.</summary>
    public string Repository { get; set; } = "csharp-style-guide";

    /// <summary>Branch (or tag/ref) to read the documents from.</summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// Path, relative to the repository root, of the folder that holds the
    /// documents and the <c>index.json</c> manifest.
    /// </summary>
    public string DesignsPath { get; set; } = "designs";

    /// <summary>
    /// Optional GitHub personal access token. Not required for public repositories,
    /// but raises the API rate limit and enables access to private repositories.
    /// </summary>
    public string? Token { get; set; }
}
