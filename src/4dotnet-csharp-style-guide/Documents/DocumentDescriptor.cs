using System.Text.Json.Serialization;

namespace FourDotNet.CSharpStyleGuide.Documents;

/// <summary>
/// A single entry from the <c>designs/index.json</c> manifest describing one
/// ADR or guideline document.
/// </summary>
public sealed class DocumentDescriptor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Path to the document, relative to the designs folder.</summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Id of the document this one supersedes, when set. Used to mark the older
    /// document as inactive and to link related documents.
    /// </summary>
    [JsonPropertyName("supersedes")]
    public string? Supersedes { get; set; }

    /// <summary>The concrete, checkable rules captured by this document.</summary>
    [JsonPropertyName("rules")]
    public IReadOnlyList<RuleDescriptor> Rules { get; set; } = [];
}

/// <summary>
/// A single concrete, checkable rule declared by a document in the manifest.
/// </summary>
public sealed class RuleDescriptor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The rule itself, stated as an imperative.</summary>
    [JsonPropertyName("rule")]
    public string Rule { get; set; } = string.Empty;

    /// <summary>Severity: <c>required</c>, <c>prohibited</c> or <c>recommended</c>.</summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    /// <summary>The area the rule applies to, e.g. <c>web-api</c>, <c>tests</c>.</summary>
    [JsonPropertyName("appliesTo")]
    public string? AppliesTo { get; set; }
}

/// <summary>Deserialized shape of <c>designs/index.json</c>.</summary>
public sealed class DocumentIndex
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("documents")]
    public IReadOnlyList<DocumentDescriptor> Documents { get; set; } = [];
}

/// <summary>A complete document: its manifest metadata plus its Markdown content.</summary>
public sealed class StyleGuideDocument
{
    public required DocumentDescriptor Descriptor { get; init; }

    public required string Content { get; init; }
}
