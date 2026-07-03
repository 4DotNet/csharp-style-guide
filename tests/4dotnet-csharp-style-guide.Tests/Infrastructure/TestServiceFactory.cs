using FourDotNet.CSharpStyleGuide.Configuration;
using FourDotNet.CSharpStyleGuide.Documents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FourDotNet.CSharpStyleGuide.Tests.Infrastructure;

/// <summary>
/// Builds a <see cref="GitHubStyleGuideDocumentService"/> wired to a
/// <see cref="StubHttpMessageHandler"/>, so tests can exercise the real service logic
/// against canned GitHub responses.
/// </summary>
internal static class TestServiceFactory
{
    /// <summary>The manifest and documents used by the service tests.</summary>
    public static IReadOnlyDictionary<string, string> DefaultResponses() => new Dictionary<string, string>
    {
        ["/index.json"] = TestData.IndexJson,
        ["/adr/0001-active.md"] = TestData.ActiveAdrBody,
        ["/adr/0002-superseded.md"] = TestData.SupersededAdrBody,
        ["/adr/0003-supersedes.md"] = TestData.SupersedesAdrBody,
        ["/adr/0004-deprecated.md"] = TestData.DeprecatedAdrBody,
        ["/guidelines/unit-testing.md"] = TestData.GuidelineBody,
    };

    public static (GitHubStyleGuideDocumentService Service, StubHttpMessageHandler Handler) Create(
        IReadOnlyDictionary<string, string>? responses = null)
    {
        var handler = new StubHttpMessageHandler(responses ?? DefaultResponses());

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(GitHubStyleGuideDocumentService.HttpClientName).Returns(httpClient);

        var options = Options.Create(new GitHubOptions
        {
            Organization = "4Dotnet",
            Repository = "csharp-style-guide",
            Branch = "main",
            DesignsPath = "designs",
        });

        var service = new GitHubStyleGuideDocumentService(
            factory,
            options,
            NullLogger<GitHubStyleGuideDocumentService>.Instance);

        return (service, handler);
    }
}
