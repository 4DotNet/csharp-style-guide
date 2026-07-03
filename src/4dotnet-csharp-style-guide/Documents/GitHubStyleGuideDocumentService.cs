using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FourDotNet.CSharpStyleGuide.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FourDotNet.CSharpStyleGuide.Documents;

/// <summary>
/// <see cref="IStyleGuideDocumentService"/> backed by the GitHub REST Contents API.
/// The <c>index.json</c> manifest and the individual documents are downloaded on
/// demand and cached for up to two hours, so updates pushed to the repository are
/// served without redeploying while GitHub is not hit on every call.
/// </summary>
public sealed partial class GitHubStyleGuideDocumentService : IStyleGuideDocumentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Maximum time the manifest and documents stay cached locally.</summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(2);

    private static readonly string[] InactiveStatuses = ["deprecated", "superseded"];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "to", "of", "in", "for", "on", "with", "how",
        "do", "does", "is", "are", "be", "using", "use", "when", "should", "my",
        "code", "csharp", "net", "dotnet", "want", "need", "write", "writing",
    };

    /// <summary>Name of the configured GitHub <see cref="HttpClient"/>.</summary>
    public const string HttpClientName = "github";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubStyleGuideDocumentService> _logger;

    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private DocumentIndex? _cachedIndex;
    private DateTimeOffset _cachedIndexExpiresAt = DateTimeOffset.MinValue;

    // Per-document content cache keyed by repository-relative path.
    private readonly ConcurrentDictionary<string, CachedDocument> _documentCache = new(StringComparer.OrdinalIgnoreCase);

    public GitHubStyleGuideDocumentService(
        IHttpClientFactory httpClientFactory,
        IOptions<GitHubOptions> options,
        ILogger<GitHubStyleGuideDocumentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentDescriptor>> ListDocumentsAsync(
        bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        return FilterActive(index.Documents, includeInactive).ToList();
    }

    public async Task<IReadOnlyList<DocumentDescriptor>> SearchDocumentsAsync(
        string keyword, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        var candidates = FilterActive(index.Documents, includeInactive);

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return candidates.ToList();
        }

        var term = keyword.Trim();
        return candidates.Where(document => MatchesMetadata(document, term)).ToList();
    }

    public async Task<IReadOnlyList<DocumentContentMatch>> SearchContentsAsync(
        string keyword, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        var term = keyword.Trim();
        var index = await GetIndexAsync(cancellationToken);
        var candidates = FilterActive(index.Documents, includeInactive).ToList();

        var matches = new List<DocumentContentMatch>();
        foreach (var descriptor in candidates)
        {
            var content = await GetContentAsync(descriptor.Path, cancellationToken);
            var snippets = ExtractSnippets(content, term);
            if (snippets.Count > 0)
            {
                matches.Add(new DocumentContentMatch(descriptor, snippets));
            }
        }

        return matches;
    }

    public async Task<IReadOnlyList<AggregatedRule>> GetRulesAsync(
        string? appliesTo = null, string? keyword = null, bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        var documents = FilterActive(index.Documents, includeInactive);

        var rules = documents.SelectMany(document => document.Rules.Select(rule => new AggregatedRule(
            rule.Id,
            rule.Rule,
            rule.Severity,
            rule.AppliesTo,
            document.Id,
            document.Title)));

        if (!string.IsNullOrWhiteSpace(appliesTo))
        {
            var area = appliesTo.Trim();
            rules = rules.Where(rule =>
                rule.AppliesTo is not null &&
                rule.AppliesTo.Contains(area, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            rules = rules.Where(rule => rule.Rule.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return rules.ToList();
    }

    public async Task<IReadOnlyList<ScoredDocument>> FindGuidanceForTaskAsync(
        string task, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(task))
        {
            return [];
        }

        var tokens = Tokenize(task);
        if (tokens.Length == 0)
        {
            return [];
        }

        var index = await GetIndexAsync(cancellationToken);
        var documents = FilterActive(index.Documents, includeInactive: false);

        var scored = new List<ScoredDocument>();
        foreach (var document in documents)
        {
            var score = 0;
            var matched = new List<string>();

            foreach (var token in tokens)
            {
                if (document.Tags.Any(tag => tag.Contains(token, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 3;
                    matched.Add(token);
                }
                else if (document.Title.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    score += 2;
                    matched.Add(token);
                }
                else if (document.Summary?.Contains(token, StringComparison.OrdinalIgnoreCase) == true)
                {
                    score += 1;
                    matched.Add(token);
                }
            }

            if (score > 0)
            {
                scored.Add(new ScoredDocument(document, score, matched.Distinct().ToList()));
            }
        }

        return scored
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxResults))
            .ToList();
    }

    public async Task<IReadOnlyList<TopicCount>> ListTopicsAsync(CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);

        return index.Documents
            .SelectMany(document => document.Tags)
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TopicCount(group.Key, group.Count()))
            .OrderByDescending(topic => topic.Count)
            .ThenBy(topic => topic.Tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<RelatedDocumentsResult?> GetRelatedDocumentsAsync(
        string id, int maxRelated = 5, CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        var documents = index.Documents;

        var self = documents.FirstOrDefault(document =>
            string.Equals(document.Id, id, StringComparison.OrdinalIgnoreCase));
        if (self is null)
        {
            return null;
        }

        var supersedes = self.Supersedes is null
            ? null
            : documents.FirstOrDefault(document =>
                string.Equals(document.Id, self.Supersedes, StringComparison.OrdinalIgnoreCase));

        var supersededBy = documents
            .Where(document => string.Equals(document.Supersedes, self.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var linked = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { self.Id };
        if (supersedes is not null)
        {
            linked.Add(supersedes.Id);
        }
        foreach (var document in supersededBy)
        {
            linked.Add(document.Id);
        }

        var relatedByTags = documents
            .Where(document => !linked.Contains(document.Id))
            .Select(document => new RelatedDocument(
                document,
                document.Tags
                    .Where(tag => self.Tags.Any(selfTag => string.Equals(selfTag, tag, StringComparison.OrdinalIgnoreCase)))
                    .ToList()))
            .Where(candidate => candidate.SharedTags.Count > 0)
            .OrderByDescending(candidate => candidate.SharedTags.Count)
            .ThenBy(candidate => candidate.Descriptor.Id, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxRelated))
            .ToList();

        return new RelatedDocumentsResult(self, supersedes, supersededBy, relatedByTags);
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

        var content = await GetContentAsync(descriptor.Path, cancellationToken);
        return new StyleGuideDocument { Descriptor = descriptor, Content = content };
    }

    public async Task<CacheRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            _cachedIndex = null;
            _cachedIndexExpiresAt = DateTimeOffset.MinValue;
            _documentCache.Clear();
        }
        finally
        {
            _indexLock.Release();
        }

        _logger.LogInformation("Cache cleared; re-downloading manifest and documents.");

        // Re-download the manifest and warm every document into the cache.
        var index = await GetIndexAsync(cancellationToken);
        await Task.WhenAll(index.Documents.Select(document =>
            GetContentAsync(document.Path, cancellationToken)));

        return new CacheRefreshResult(index.Documents.Count);
    }

    private static IEnumerable<DocumentDescriptor> FilterActive(
        IReadOnlyList<DocumentDescriptor> documents, bool includeInactive)
    {
        if (includeInactive)
        {
            return documents;
        }

        var superseded = documents
            .Where(document => document.Supersedes is not null)
            .Select(document => document.Supersedes!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return documents.Where(document =>
            !InactiveStatuses.Contains(document.Status, StringComparer.OrdinalIgnoreCase) &&
            !superseded.Contains(document.Id));
    }

    private static bool MatchesMetadata(DocumentDescriptor document, string term)
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

    private static IReadOnlyList<string> ExtractSnippets(string content, string term, int maxSnippets = 5)
    {
        var snippets = new List<string>();
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || !line.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            snippets.Add(line.Length > 240 ? line[..240].TrimEnd() + "…" : line);
            if (snippets.Count >= maxSnippets)
            {
                break;
            }
        }

        return snippets;
    }

    private static string[] Tokenize(string text) =>
        TokenRegex().Matches(text.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(word => word.Length > 2 && !StopWords.Contains(word))
            .Distinct()
            .ToArray();

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
            _cachedIndexExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration);
            _logger.LogInformation("Loaded style-guide manifest with {DocumentCount} document(s).", index.Documents.Count);
            return index;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    /// <summary>Returns a document's Markdown body from the cache, downloading it when stale.</summary>
    private async Task<string> GetContentAsync(string documentPath, CancellationToken cancellationToken)
    {
        if (_documentCache.TryGetValue(documentPath, out var cached) &&
            DateTimeOffset.UtcNow < cached.ExpiresAt)
        {
            return cached.Content;
        }

        var content = await FetchRawAsync(BuildDesignsPath(documentPath), cancellationToken);
        _documentCache[documentPath] = new CachedDocument(content, DateTimeOffset.UtcNow.Add(CacheDuration));
        return content;
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

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);

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

    [GeneratedRegex("[a-z0-9#.]+")]
    private static partial Regex TokenRegex();

    private readonly record struct CachedDocument(string Content, DateTimeOffset ExpiresAt);
}
