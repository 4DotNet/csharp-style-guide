using System.Net;

namespace FourDotNet.CSharpStyleGuide.Tests.Infrastructure;

/// <summary>
/// A test <see cref="HttpMessageHandler"/> that serves canned responses keyed by the
/// path segment of the request URI and records how many requests it received. Used to
/// drive <c>GitHubStyleGuideDocumentService</c> without hitting the real GitHub API.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly IReadOnlyDictionary<string, string> _responsesByPath;

    /// <summary>Number of requests sent through this handler, per absolute path.</summary>
    public Dictionary<string, int> RequestCountsByPath { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Total number of requests sent through this handler.</summary>
    public int TotalRequests { get; private set; }

    public StubHttpMessageHandler(IReadOnlyDictionary<string, string> responsesByPath)
    {
        _responsesByPath = responsesByPath;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        TotalRequests++;
        var path = request.RequestUri!.AbsolutePath;
        RequestCountsByPath[path] = RequestCountsByPath.GetValueOrDefault(path) + 1;

        var match = _responsesByPath
            .FirstOrDefault(entry => path.EndsWith(entry.Key, StringComparison.OrdinalIgnoreCase));

        if (match.Value is null)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request,
                Content = new StringContent(string.Empty),
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StringContent(match.Value),
        });
    }
}
