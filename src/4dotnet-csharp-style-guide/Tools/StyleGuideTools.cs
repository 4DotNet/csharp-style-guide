using System.ComponentModel;
using FourDotNet.CSharpStyleGuide.Documents;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FourDotNet.CSharpStyleGuide.Tools;

/// <summary>
/// MCP tools that expose the 4DotNet C# style guide (ADRs and guidelines) to clients.
/// </summary>
[McpServerToolType]
public sealed class StyleGuideTools
{
    private readonly IStyleGuideDocumentService _documents;

    public StyleGuideTools(IStyleGuideDocumentService documents)
    {
        _documents = documents;
    }

    [McpServerTool(Name = "list_documents")]
    [Description("Lists all C# style-guide documents (ADRs and guidelines) with their metadata. " +
                 "Returns each document's id, type, title, status, date, summary and tags. " +
                 "Use the id with get_document to read the full content.")]
    public async Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _documents.ListDocumentsAsync(cancellationToken);
        return documents.Select(DocumentSummary.From).ToList();
    }

    [McpServerTool(Name = "search_documents")]
    [Description("Searches the C# style-guide documents by keyword. Matches the keyword " +
                 "case-insensitively against each document's id, title, summary, type, status and tags. " +
                 "Returns the matching documents' metadata; use the id with get_document to read the full content.")]
    public async Task<IReadOnlyList<DocumentSummary>> SearchDocumentsAsync(
        [Description("The keyword to search for, e.g. 'testing', 'net10' or 'xunit'.")] string keyword,
        CancellationToken cancellationToken = default)
    {
        var documents = await _documents.SearchDocumentsAsync(keyword, cancellationToken);
        return documents.Select(DocumentSummary.From).ToList();
    }

    [McpServerTool(Name = "get_document")]
    [Description("Returns a single complete C# style-guide document, including its full " +
                 "Markdown content, identified by its id (as returned by list_documents or search_documents).")]
    public async Task<DocumentContent> GetDocumentAsync(
        [Description("The id of the document to retrieve, e.g. 'adr-0001' or 'guideline-unit-testing'.")] string id,
        CancellationToken cancellationToken = default)
    {
        var document = await _documents.GetDocumentAsync(id, cancellationToken)
            ?? throw new McpException($"No style-guide document was found with id '{id}'.");

        return DocumentContent.From(document);
    }

    /// <summary>Metadata projection returned by the list/search tools.</summary>
    public sealed record DocumentSummary(
        string Id,
        string Type,
        string Title,
        string Status,
        string? Date,
        string? Summary,
        IReadOnlyList<string> Tags)
    {
        public static DocumentSummary From(DocumentDescriptor descriptor) => new(
            descriptor.Id,
            descriptor.Type,
            descriptor.Title,
            descriptor.Status,
            descriptor.Date,
            descriptor.Summary,
            descriptor.Tags);
    }

    /// <summary>A complete document (metadata + Markdown) returned by get_document.</summary>
    public sealed record DocumentContent(
        string Id,
        string Type,
        string Title,
        string Status,
        string? Date,
        IReadOnlyList<string> Tags,
        string Content)
    {
        public static DocumentContent From(StyleGuideDocument document) => new(
            document.Descriptor.Id,
            document.Descriptor.Type,
            document.Descriptor.Title,
            document.Descriptor.Status,
            document.Descriptor.Date,
            document.Descriptor.Tags,
            document.Content);
    }
}
