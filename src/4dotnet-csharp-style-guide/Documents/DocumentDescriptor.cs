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
