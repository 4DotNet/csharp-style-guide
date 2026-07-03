namespace FourDotNet.CSharpStyleGuide.Documents;

/// <summary>A document whose body matched a full-text search, with matching snippets.</summary>
public sealed record DocumentContentMatch(
    DocumentDescriptor Descriptor,
    IReadOnlyList<string> Snippets);

/// <summary>A rule flattened out of a document, carrying its source for traceability.</summary>
public sealed record AggregatedRule(
    string RuleId,
    string Rule,
    string Severity,
    string? AppliesTo,
    string SourceDocumentId,
    string SourceDocumentTitle);

/// <summary>A tag/topic and how many documents carry it.</summary>
public sealed record TopicCount(string Tag, int Count);

/// <summary>A document ranked by how well it matches a task description.</summary>
public sealed record ScoredDocument(
    DocumentDescriptor Descriptor,
    int Score,
    IReadOnlyList<string> MatchedTerms);

/// <summary>Another document related to a given one, with the tags they share.</summary>
public sealed record RelatedDocument(
    DocumentDescriptor Descriptor,
    IReadOnlyList<string> SharedTags);

/// <summary>The relationships around a single document.</summary>
public sealed record RelatedDocumentsResult(
    DocumentDescriptor Document,
    DocumentDescriptor? Supersedes,
    IReadOnlyList<DocumentDescriptor> SupersededBy,
    IReadOnlyList<RelatedDocument> RelatedByTags);

/// <summary>Outcome of a cache refresh.</summary>
public sealed record CacheRefreshResult(int DocumentCount);
