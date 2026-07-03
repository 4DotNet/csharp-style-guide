using FourDotNet.CSharpStyleGuide.Tests.Infrastructure;
using Xunit;

namespace FourDotNet.CSharpStyleGuide.Tests;

/// <summary>
/// Tests the document service logic — filtering, searching, ranking, relationships and
/// caching — against a stubbed GitHub Contents API.
/// </summary>
public sealed class GitHubStyleGuideDocumentServiceTests
{
    [Fact]
    public async Task ListDocuments_excludes_superseded_and_deprecated_by_default()
    {
        var (service, _) = TestServiceFactory.Create();

        var documents = await service.ListDocumentsAsync();

        var ids = documents.Select(d => d.Id).ToArray();
        Assert.Equal(new[] { "adr-0001", "adr-0003", "guideline-unit-testing" }, ids.OrderBy(id => id));
        Assert.DoesNotContain("adr-0002", ids); // superseded by adr-0003
        Assert.DoesNotContain("adr-0004", ids); // status deprecated
    }

    [Fact]
    public async Task ListDocuments_includes_inactive_when_requested()
    {
        var (service, _) = TestServiceFactory.Create();

        var documents = await service.ListDocumentsAsync(includeInactive: true);

        Assert.Equal(5, documents.Count);
    }

    [Fact]
    public async Task SearchDocuments_matches_metadata_case_insensitively()
    {
        var (service, _) = TestServiceFactory.Create();

        var results = await service.SearchDocumentsAsync("ARCHITECTURE");

        var document = Assert.Single(results);
        Assert.Equal("adr-0003", document.Id);
    }

    [Fact]
    public async Task SearchDocuments_with_blank_keyword_returns_all_active()
    {
        var (service, _) = TestServiceFactory.Create();

        var results = await service.SearchDocumentsAsync("   ");

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task SearchContents_returns_snippets_for_body_matches()
    {
        var (service, _) = TestServiceFactory.Create();

        var matches = await service.SearchContentsAsync("aggregate");

        var match = Assert.Single(matches);
        Assert.Equal("adr-0001", match.Descriptor.Id);
        Assert.Contains(match.Snippets, snippet => snippet.Contains("aggregate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchContents_with_blank_keyword_returns_empty()
    {
        var (service, handler) = TestServiceFactory.Create();

        var matches = await service.SearchContentsAsync("");

        Assert.Empty(matches);
        // A blank keyword short-circuits before any GitHub call.
        Assert.Equal(0, handler.TotalRequests);
    }

    [Fact]
    public async Task GetRules_flattens_rules_from_active_documents()
    {
        var (service, _) = TestServiceFactory.Create();

        var rules = await service.GetRulesAsync();

        Assert.Equal(3, rules.Count);
        Assert.All(rules, rule => Assert.False(string.IsNullOrEmpty(rule.SourceDocumentId)));
        Assert.Contains(rules, rule => rule.RuleId == "adr-0001-r1" && rule.SourceDocumentTitle == "Pragmatic DDD domain models");
    }

    [Fact]
    public async Task GetRules_filters_by_appliesTo_and_keyword()
    {
        var (service, _) = TestServiceFactory.Create();

        var byArea = await service.GetRulesAsync(appliesTo: "web-api");
        Assert.Equal(new[] { "adr-0001-r1" }, byArea.Select(r => r.RuleId));

        var byKeyword = await service.GetRulesAsync(keyword: "unit test");
        Assert.Equal(new[] { "guideline-unit-testing-r1" }, byKeyword.Select(r => r.RuleId));
    }

    [Fact]
    public async Task FindGuidanceForTask_ranks_tag_matches_above_summary_matches()
    {
        var (service, _) = TestServiceFactory.Create();

        // "testing" is a tag on adr-0001 and the guideline (score 3 each);
        // "handlers" only appears in the guideline summary (score 1).
        var results = await service.FindGuidanceForTaskAsync("writing testing for handlers");

        Assert.NotEmpty(results);
        // The guideline matches on both a tag and its summary, so it should rank first.
        Assert.Equal("guideline-unit-testing", results[0].Descriptor.Id);
        Assert.All(results, r => Assert.True(r.Score > 0));
    }

    [Fact]
    public async Task FindGuidanceForTask_returns_empty_when_only_stopwords()
    {
        var (service, _) = TestServiceFactory.Create();

        var results = await service.FindGuidanceForTaskAsync("how do I write my code");

        Assert.Empty(results);
    }

    [Fact]
    public async Task FindGuidanceForTask_honors_maxResults()
    {
        var (service, _) = TestServiceFactory.Create();

        var results = await service.FindGuidanceForTaskAsync("web-api testing architecture", maxResults: 1);

        Assert.Single(results);
    }

    [Fact]
    public async Task ListTopics_counts_tags_across_all_documents()
    {
        var (service, _) = TestServiceFactory.Create();

        var topics = await service.ListTopicsAsync();

        var webApi = Assert.Single(topics, t => t.Tag == "web-api");
        Assert.Equal(3, webApi.Count); // adr-0001, adr-0002, adr-0003

        var testing = Assert.Single(topics, t => t.Tag == "testing");
        Assert.Equal(2, testing.Count); // adr-0001, guideline

        // Ordered by descending count, so the most common tag comes first.
        Assert.Equal("web-api", topics[0].Tag);
    }

    [Fact]
    public async Task GetRelatedDocuments_resolves_supersedes_and_superseded_by()
    {
        var (service, _) = TestServiceFactory.Create();

        var relatedForNew = await service.GetRelatedDocumentsAsync("adr-0003");
        Assert.NotNull(relatedForNew);
        Assert.Equal("adr-0002", relatedForNew!.Supersedes?.Id);

        var relatedForOld = await service.GetRelatedDocumentsAsync("adr-0002");
        Assert.NotNull(relatedForOld);
        Assert.Contains(relatedForOld!.SupersededBy, d => d.Id == "adr-0003");
    }

    [Fact]
    public async Task GetRelatedDocuments_ranks_by_shared_tags_excluding_linked()
    {
        var (service, _) = TestServiceFactory.Create();

        var related = await service.GetRelatedDocumentsAsync("adr-0001");

        Assert.NotNull(related);
        // adr-0001 shares "web-api" with adr-0003 and "testing" with the guideline.
        var relatedIds = related!.RelatedByTags.Select(r => r.Descriptor.Id).ToArray();
        Assert.Contains("adr-0003", relatedIds);
        Assert.Contains("guideline-unit-testing", relatedIds);
        // The subject itself must never appear among its related documents.
        Assert.DoesNotContain("adr-0001", relatedIds);
    }

    [Fact]
    public async Task GetRelatedDocuments_returns_null_for_unknown_id()
    {
        var (service, _) = TestServiceFactory.Create();

        var related = await service.GetRelatedDocumentsAsync("does-not-exist");

        Assert.Null(related);
    }

    [Fact]
    public async Task GetDocument_returns_metadata_and_body()
    {
        var (service, _) = TestServiceFactory.Create();

        var document = await service.GetDocumentAsync("adr-0001");

        Assert.NotNull(document);
        Assert.Equal("adr-0001", document!.Descriptor.Id);
        Assert.Contains("aggregate", document.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDocument_returns_null_for_unknown_id()
    {
        var (service, _) = TestServiceFactory.Create();

        var document = await service.GetDocumentAsync("nope");

        Assert.Null(document);
    }

    [Fact]
    public async Task Refresh_reloads_manifest_and_reports_document_count()
    {
        var (service, handler) = TestServiceFactory.Create();

        var result = await service.RefreshAsync();

        Assert.Equal(5, result.DocumentCount);
        // Refresh warms every document plus the manifest.
        Assert.True(handler.TotalRequests >= 6);
    }

    [Fact]
    public async Task Manifest_is_cached_across_calls()
    {
        var (service, handler) = TestServiceFactory.Create();

        await service.ListDocumentsAsync();
        await service.ListDocumentsAsync();
        await service.ListTopicsAsync();

        // The manifest must be fetched only once and then served from cache.
        Assert.Equal(1, handler.RequestCountsByPath.Single(kv => kv.Key.EndsWith("index.json")).Value);
    }

    [Fact]
    public async Task Document_body_is_cached_across_calls()
    {
        var (service, handler) = TestServiceFactory.Create();

        await service.GetDocumentAsync("adr-0001");
        await service.GetDocumentAsync("adr-0001");

        var bodyRequests = handler.RequestCountsByPath.Single(kv => kv.Key.EndsWith("0001-active.md")).Value;
        Assert.Equal(1, bodyRequests);
    }
}
