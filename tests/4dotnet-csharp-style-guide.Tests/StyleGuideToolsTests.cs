using FourDotNet.CSharpStyleGuide.Documents;
using FourDotNet.CSharpStyleGuide.Tools;
using ModelContextProtocol;
using NSubstitute;
using Xunit;

namespace FourDotNet.CSharpStyleGuide.Tests;

/// <summary>
/// Tests that the MCP tool layer correctly projects the service results into the
/// response records and enforces its not-found contract, using a faked service.
/// </summary>
public sealed class StyleGuideToolsTests
{
    private static DocumentDescriptor Descriptor(string id, string title = "Title") => new()
    {
        Id = id,
        Type = "adr",
        Title = title,
        Path = $"adr/{id}.md",
        Status = "accepted",
        Date = "2026-01-01",
        Summary = "A summary.",
        Tags = ["testing", "web-api"],
    };

    [Fact]
    public async Task ListDocuments_projects_descriptors_and_forwards_includeInactive()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.ListDocumentsAsync(true, Arg.Any<CancellationToken>())
            .Returns([Descriptor("adr-0001", "First")]);
        var tools = new StyleGuideTools(service);

        var result = await tools.ListDocumentsAsync(includeInactive: true);

        var summary = Assert.Single(result);
        Assert.Equal("adr-0001", summary.Id);
        Assert.Equal("First", summary.Title);
        Assert.Equal(["testing", "web-api"], summary.Tags);
        await service.Received(1).ListDocumentsAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchDocumentContents_projects_matches_and_snippets()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.SearchContentsAsync("aggregate", false, Arg.Any<CancellationToken>())
            .Returns([new DocumentContentMatch(Descriptor("adr-0001"), ["line with aggregate"])]);
        var tools = new StyleGuideTools(service);

        var result = await tools.SearchDocumentContentsAsync("aggregate");

        var match = Assert.Single(result);
        Assert.Equal("adr-0001", match.Document.Id);
        Assert.Equal(["line with aggregate"], match.Snippets);
    }

    [Fact]
    public async Task GetRules_projects_aggregated_rules()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.GetRulesAsync(null, null, false, Arg.Any<CancellationToken>())
            .Returns([new AggregatedRule("r1", "Validate input.", "required", "web-api", "adr-0001", "First")]);
        var tools = new StyleGuideTools(service);

        var result = await tools.GetRulesAsync();

        var rule = Assert.Single(result);
        Assert.Equal("r1", rule.RuleId);
        Assert.Equal("required", rule.Severity);
        Assert.Equal("adr-0001", rule.SourceDocumentId);
    }

    [Fact]
    public async Task FindGuidanceForTask_projects_scored_documents()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.FindGuidanceForTaskAsync("cart endpoint", 5, Arg.Any<CancellationToken>())
            .Returns([new ScoredDocument(Descriptor("adr-0001"), 5, ["cart"])]);
        var tools = new StyleGuideTools(service);

        var result = await tools.FindGuidanceForTaskAsync("cart endpoint");

        var match = Assert.Single(result);
        Assert.Equal(5, match.Score);
        Assert.Equal(["cart"], match.MatchedTerms);
    }

    [Fact]
    public async Task ListTopics_projects_topic_counts()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.ListTopicsAsync(Arg.Any<CancellationToken>())
            .Returns([new TopicCount("web-api", 3)]);
        var tools = new StyleGuideTools(service);

        var result = await tools.ListTopicsAsync();

        var topic = Assert.Single(result);
        Assert.Equal("web-api", topic.Tag);
        Assert.Equal(3, topic.Count);
    }

    [Fact]
    public async Task RelatedDocuments_projects_relationships()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        var result = new RelatedDocumentsResult(
            Descriptor("adr-0003"),
            Descriptor("adr-0002"),
            [],
            [new RelatedDocument(Descriptor("adr-0001"), ["web-api"])]);
        service.GetRelatedDocumentsAsync("adr-0003", 5, Arg.Any<CancellationToken>()).Returns(result);
        var tools = new StyleGuideTools(service);

        var view = await tools.RelatedDocumentsAsync("adr-0003");

        Assert.Equal("adr-0003", view.Document.Id);
        Assert.Equal("adr-0002", view.Supersedes?.Id);
        var related = Assert.Single(view.RelatedByTags);
        Assert.Equal("adr-0001", related.Document.Id);
        Assert.Equal(["web-api"], related.SharedTags);
    }

    [Fact]
    public async Task RelatedDocuments_throws_McpException_when_not_found()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.GetRelatedDocumentsAsync("missing", 5, Arg.Any<CancellationToken>())
            .Returns((RelatedDocumentsResult?)null);
        var tools = new StyleGuideTools(service);

        var exception = await Assert.ThrowsAsync<McpException>(() => tools.RelatedDocumentsAsync("missing"));
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public async Task GetDocument_projects_full_document()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.GetDocumentAsync("adr-0001", Arg.Any<CancellationToken>())
            .Returns(new StyleGuideDocument { Descriptor = Descriptor("adr-0001"), Content = "# Body" });
        var tools = new StyleGuideTools(service);

        var document = await tools.GetDocumentAsync("adr-0001");

        Assert.Equal("adr-0001", document.Id);
        Assert.Equal("# Body", document.Content);
        Assert.Equal(["testing", "web-api"], document.Tags);
    }

    [Fact]
    public async Task GetDocument_throws_McpException_when_not_found()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.GetDocumentAsync("missing", Arg.Any<CancellationToken>())
            .Returns((StyleGuideDocument?)null);
        var tools = new StyleGuideTools(service);

        var exception = await Assert.ThrowsAsync<McpException>(() => tools.GetDocumentAsync("missing"));
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public async Task RefreshCache_reports_reloaded_document_count()
    {
        var service = Substitute.For<IStyleGuideDocumentService>();
        service.RefreshAsync(Arg.Any<CancellationToken>()).Returns(new CacheRefreshResult(7));
        var tools = new StyleGuideTools(service);

        var result = await tools.RefreshCacheAsync();

        Assert.Equal(7, result.DocumentCount);
        Assert.Contains("7", result.Message);
    }
}
