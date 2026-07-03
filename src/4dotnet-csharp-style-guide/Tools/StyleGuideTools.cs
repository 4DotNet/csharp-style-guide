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
                 "Superseded and deprecated documents are excluded unless includeInactive is true. " +
                 "Use the id with get_document to read the full content.")]
    public async Task<IReadOnlyList<DocumentSummary>> ListDocumentsAsync(
        [Description("Include deprecated and superseded documents. Defaults to false.")] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var documents = await _documents.ListDocumentsAsync(includeInactive, cancellationToken);
        return documents.Select(DocumentSummary.From).ToList();
    }

    [McpServerTool(Name = "search_documents")]
    [Description("Searches the C# style-guide documents by keyword over their METADATA " +
                 "(id, title, summary, type, status and tags), case-insensitively. " +
                 "For matches inside the document body, use search_document_contents instead. " +
                 "Superseded and deprecated documents are excluded unless includeInactive is true.")]
    public async Task<IReadOnlyList<DocumentSummary>> SearchDocumentsAsync(
        [Description("The keyword to search for, e.g. 'testing', 'net10' or 'xunit'.")] string keyword,
        [Description("Include deprecated and superseded documents. Defaults to false.")] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var documents = await _documents.SearchDocumentsAsync(keyword, includeInactive, cancellationToken);
        return documents.Select(DocumentSummary.From).ToList();
    }

    [McpServerTool(Name = "search_document_contents")]
    [Description("Full-text search across the Markdown BODY of every style-guide document. " +
                 "Returns each matching document's metadata plus the matching lines as snippets. " +
                 "Use this when the term you are looking for is code or prose that may only appear " +
                 "inside a document, not in its title/summary/tags.")]
    public async Task<IReadOnlyList<ContentMatch>> SearchDocumentContentsAsync(
        [Description("The keyword or phrase to look for inside document bodies.")] string keyword,
        [Description("Include deprecated and superseded documents. Defaults to false.")] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var matches = await _documents.SearchContentsAsync(keyword, includeInactive, cancellationToken);
        return matches.Select(ContentMatch.From).ToList();
    }

    [McpServerTool(Name = "get_rules")]
    [Description("Returns the concrete, checkable rules declared across the style guide — the " +
                 "do's and don'ts (required / prohibited / recommended) — each linked back to the " +
                 "document that defines it. Optionally filter by the area a rule applies to " +
                 "(e.g. 'web-api', 'tests') and/or a keyword. Ideal for quickly checking generated " +
                 "code against the standards.")]
    public async Task<IReadOnlyList<RuleView>> GetRulesAsync(
        [Description("Optional area filter, e.g. 'web-api', 'tests', 'domain-model'.")] string? appliesTo = null,
        [Description("Optional keyword to match within the rule text.")] string? keyword = null,
        [Description("Include rules from deprecated/superseded documents. Defaults to false.")] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var rules = await _documents.GetRulesAsync(appliesTo, keyword, includeInactive, cancellationToken);
        return rules.Select(RuleView.From).ToList();
    }

    [McpServerTool(Name = "find_guidance_for_task")]
    [Description("Given a free-text description of what you are working on (e.g. 'creating a new " +
                 "HTTP endpoint for carts' or 'writing unit tests for a handler'), returns the most " +
                 "relevant style-guide documents ranked by relevance, with the terms that matched. " +
                 "Use the returned ids with get_document to read the full guidance.")]
    public async Task<IReadOnlyList<GuidanceMatch>> FindGuidanceForTaskAsync(
        [Description("A description of the task or code you are about to write.")] string task,
        [Description("Maximum number of documents to return. Defaults to 5.")] int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        var scored = await _documents.FindGuidanceForTaskAsync(task, maxResults, cancellationToken);
        return scored.Select(GuidanceMatch.From).ToList();
    }

    [McpServerTool(Name = "list_topics")]
    [Description("Lists every topic/tag used across the style guide together with how many " +
                 "documents carry it. Use this to discover what the guide covers before searching.")]
    public async Task<IReadOnlyList<TopicView>> ListTopicsAsync(CancellationToken cancellationToken = default)
    {
        var topics = await _documents.ListTopicsAsync(cancellationToken);
        return topics.Select(topic => new TopicView(topic.Tag, topic.Count)).ToList();
    }

    [McpServerTool(Name = "related_documents")]
    [Description("Returns documents related to the given document id: what it supersedes, what " +
                 "supersedes it, and the documents that share the most tags. Use this to explore " +
                 "connected guidance around a decision.")]
    public async Task<RelatedView> RelatedDocumentsAsync(
        [Description("The id of the document to find relations for, e.g. 'adr-0005'.")] string id,
        [Description("Maximum number of tag-related documents to return. Defaults to 5.")] int maxRelated = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _documents.GetRelatedDocumentsAsync(id, maxRelated, cancellationToken)
            ?? throw new McpException($"No style-guide document was found with id '{id}'.");

        return RelatedView.From(result);
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

    [McpServerTool(Name = "refresh_cache")]
    [Description("Clears the server's local cache and immediately re-downloads the manifest and " +
                 "every document from GitHub, so subsequent calls serve the very latest content. " +
                 "Documents are otherwise cached for up to two hours. Returns how many documents were reloaded.")]
    public async Task<CacheRefreshView> RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        var result = await _documents.RefreshAsync(cancellationToken);
        return new CacheRefreshView(result.DocumentCount, $"Reloaded {result.DocumentCount} document(s) from GitHub.");
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

    /// <summary>A body-search hit: the document plus its matching snippets.</summary>
    public sealed record ContentMatch(DocumentSummary Document, IReadOnlyList<string> Snippets)
    {
        public static ContentMatch From(DocumentContentMatch match) =>
            new(DocumentSummary.From(match.Descriptor), match.Snippets);
    }

    /// <summary>A rule with a link back to the document that defines it.</summary>
    public sealed record RuleView(
        string RuleId,
        string Rule,
        string Severity,
        string? AppliesTo,
        string SourceDocumentId,
        string SourceDocumentTitle)
    {
        public static RuleView From(AggregatedRule rule) => new(
            rule.RuleId,
            rule.Rule,
            rule.Severity,
            rule.AppliesTo,
            rule.SourceDocumentId,
            rule.SourceDocumentTitle);
    }

    /// <summary>A relevance-ranked document for a task.</summary>
    public sealed record GuidanceMatch(DocumentSummary Document, int Score, IReadOnlyList<string> MatchedTerms)
    {
        public static GuidanceMatch From(ScoredDocument scored) =>
            new(DocumentSummary.From(scored.Descriptor), scored.Score, scored.MatchedTerms);
    }

    /// <summary>A topic/tag with its document count.</summary>
    public sealed record TopicView(string Tag, int Count);

    /// <summary>The relationships around a single document.</summary>
    public sealed record RelatedView(
        DocumentSummary Document,
        DocumentSummary? Supersedes,
        IReadOnlyList<DocumentSummary> SupersededBy,
        IReadOnlyList<RelatedDocumentView> RelatedByTags)
    {
        public static RelatedView From(RelatedDocumentsResult result) => new(
            DocumentSummary.From(result.Document),
            result.Supersedes is null ? null : DocumentSummary.From(result.Supersedes),
            result.SupersededBy.Select(DocumentSummary.From).ToList(),
            result.RelatedByTags
                .Select(related => new RelatedDocumentView(DocumentSummary.From(related.Descriptor), related.SharedTags))
                .ToList());
    }

    /// <summary>A related document plus the tags it shares with the subject.</summary>
    public sealed record RelatedDocumentView(DocumentSummary Document, IReadOnlyList<string> SharedTags);

    /// <summary>Result of a cache refresh.</summary>
    public sealed record CacheRefreshView(int DocumentCount, string Message);

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
