namespace FourDotNet.CSharpStyleGuide.Documents;

/// <summary>
/// Provides access to the style-guide documents. The manifest and the individual
/// documents are fetched from GitHub at runtime and cached locally, so content can
/// be added or updated in the repository without releasing a new version of this
/// server. By default, superseded and deprecated documents are excluded.
/// </summary>
public interface IStyleGuideDocumentService
{
    /// <summary>Lists the metadata of every document in the manifest.</summary>
    Task<IReadOnlyList<DocumentDescriptor>> ListDocumentsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the documents whose metadata (id, title, summary, tags, type or
    /// status) matches the given keyword, case-insensitively.
    /// </summary>
    Task<IReadOnlyList<DocumentDescriptor>> SearchDocumentsAsync(
        string keyword,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the documents whose Markdown <em>body</em> contains the keyword,
    /// together with the matching lines as snippets.
    /// </summary>
    Task<IReadOnlyList<DocumentContentMatch>> SearchContentsAsync(
        string keyword,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the concrete rules declared across the documents, optionally filtered
    /// by the area they apply to and/or a keyword.
    /// </summary>
    Task<IReadOnlyList<AggregatedRule>> GetRulesAsync(
        string? appliesTo = null,
        string? keyword = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ranks the documents by how well their metadata matches a free-text
    /// description of the task the caller is working on.
    /// </summary>
    Task<IReadOnlyList<ScoredDocument>> FindGuidanceForTaskAsync(
        string task,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    /// <summary>Returns every tag/topic used in the guide and how many documents use it.</summary>
    Task<IReadOnlyList<TopicCount>> ListTopicsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the documents related to the given id: what it supersedes, what
    /// supersedes it, and the documents that share the most tags. Returns
    /// <c>null</c> when no document with that id exists.
    /// </summary>
    Task<RelatedDocumentsResult?> GetRelatedDocumentsAsync(
        string id,
        int maxRelated = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single complete document (metadata + Markdown content) by its id,
    /// or <c>null</c> when no document with that id exists in the manifest.
    /// </summary>
    Task<StyleGuideDocument?> GetDocumentAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the local cache and re-downloads the manifest and every document,
    /// so subsequent calls serve the latest content from GitHub.
    /// </summary>
    Task<CacheRefreshResult> RefreshAsync(CancellationToken cancellationToken = default);
}
