namespace FourDotNet.CSharpStyleGuide.Documents;

/// <summary>
/// Provides access to the style-guide documents. Documents are fetched from
/// GitHub at runtime so that content can be added or updated in the repository
/// without releasing a new version of this server.
/// </summary>
public interface IStyleGuideDocumentService
{
    /// <summary>Lists the metadata of every document in the manifest.</summary>
    Task<IReadOnlyList<DocumentDescriptor>> ListDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the documents whose metadata (id, title, summary, tags, type or
    /// status) matches the given keyword, case-insensitively.
    /// </summary>
    Task<IReadOnlyList<DocumentDescriptor>> SearchDocumentsAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single complete document (metadata + Markdown content) by its id,
    /// or <c>null</c> when no document with that id exists in the manifest.
    /// </summary>
    Task<StyleGuideDocument?> GetDocumentAsync(string id, CancellationToken cancellationToken = default);
}
