using System.Net;
using System.Text.Json;
using FourDotNet.CSharpStyleGuide.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FourDotNet.CSharpStyleGuide.Documents;

/// <summary>
/// <see cref="IStyleGuideDocumentService"/> backed by the GitHub REST Contents API.
/// The <c>index.json</c> manifest and the individual documents are downloaded on
/// demand, so updates pushed to the repository are served without redeploying.
/// </summary>
public sealed class GitHubStyleGuideDocumentService : IStyleGuideDocumentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan IndexCacheDuration = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubStyleGuideDocumentService> _logger;

    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private DocumentIndex? _cachedIndex;
    private DateTimeOffset _cachedIndexExpiresAt = DateTimeOffset.MinValue;

    public GitHubStyleGuideDocumentService(
        HttpClient httpClient,
        IOptions<GitHubOptions> options,
        ILogger<GitHubStyleGuideDocumentService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentDescriptor>> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        return index.Documents;
    }

    public async Task<IReadOnlyList<DocumentDescriptor>> SearchDocumentsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return index.Documents;
        }

        var term = keyword.Trim();
        return index.Documents
            .Where(document => Matches(document, term))
            .ToList();
    }

    public async Task<StyleGuideDocument?> GetDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);

        var descriptor = index.Documents
            .FirstOrDefault(document => string.Equals(document.Id, id, StringComparison.OrdinalIgnoreCase));

        if (descriptor is null)
        {
            _logger.LogInformation("No document found with id '{DocumentId}'.", id);
            return null;
        }

        var content = await FetchRawAsync(BuildDesignsPath(descriptor.Path), cancellationToken);
        return new StyleGuideDocument { Descriptor = descriptor, Content = content };
    }

    private static bool Matches(DocumentDescriptor document, string term)
    {
        bool Contains(string? value) =>
            value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

        return Contains(document.Id)
            || Contains(document.Title)
            || Contains(document.Summary)
            || Contains(document.Type)
            || Contains(document.Status)
            || document.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<DocumentIndex> GetIndexAsync(CancellationToken cancellationToken)
    {
        if (_cachedIndex is not null && DateTimeOffset.UtcNow < _cachedIndexExpiresAt)
        {
            return _cachedIndex;
        }

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedIndex is not null && DateTimeOffset.UtcNow < _cachedIndexExpiresAt)
            {
                return _cachedIndex;
            }

            var json = await FetchRawAsync(BuildDesignsPath("index.json"), cancellationToken);
            var index = JsonSerializer.Deserialize<DocumentIndex>(json, JsonOptions)
                ?? throw new InvalidOperationException("The style-guide index.json could not be parsed.");

            _cachedIndex = index;
            _cachedIndexExpiresAt = DateTimeOffset.UtcNow.Add(IndexCacheDuration);
            _logger.LogInformation("Loaded style-guide manifest with {DocumentCount} document(s).", index.Documents.Count);
            return index;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    /// <summary>
    /// Downloads a file from the repository via the GitHub Contents API, requesting
    /// the raw representation so the body is the file content itself.
    /// </summary>
    private async Task<string> FetchRawAsync(string repositoryRelativePath, CancellationToken cancellationToken)
    {
        // GET /repos/{owner}/{repo}/contents/{path}?ref={branch}
        var requestUri =
            $"repos/{_options.Organization}/{_options.Repository}/contents/{repositoryRelativePath}?ref={Uri.EscapeDataString(_options.Branch)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        // Ask GitHub to return the raw file bytes rather than the base64 JSON envelope.
        request.Headers.Accept.ParseAdd("application/vnd.github.raw+json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException(
                $"'{repositoryRelativePath}' was not found in {_options.Organization}/{_options.Repository}@{_options.Branch}.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private string BuildDesignsPath(string relativePath)
    {
        var designsPath = _options.DesignsPath.Trim('/');
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        return string.IsNullOrEmpty(designsPath) ? normalized : $"{designsPath}/{normalized}";
    }
}
