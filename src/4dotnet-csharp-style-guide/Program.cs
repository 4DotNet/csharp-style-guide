using System.Net.Http.Headers;
using FourDotNet.CSharpStyleGuide.Configuration;
using FourDotNet.CSharpStyleGuide.Documents;
using FourDotNet.CSharpStyleGuide.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

var builder = Host.CreateApplicationBuilder(args);

// stdio transport uses stdout for the JSON-RPC channel, so all logging must go to stderr.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// Bind the "GitHub" configuration section (organization + repository etc.).
builder.Services
    .AddOptions<GitHubOptions>()
    .Bind(builder.Configuration.GetSection(GitHubOptions.SectionName));

// Named HttpClient pointed at the GitHub REST API, configured from GitHubOptions.
builder.Services.AddHttpClient(GitHubStyleGuideDocumentService.HttpClientName, (provider, client) =>
{
    var options = provider.GetRequiredService<IOptions<GitHubOptions>>().Value;

    client.BaseAddress = new Uri(options.ApiBaseUrl);
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("4dotnet-csharp-style-guide", "1.0"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

    if (!string.IsNullOrWhiteSpace(options.Token))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
    }
});

// Singleton so the 2-hour document/manifest cache is shared across all tool calls.
builder.Services.AddSingleton<IStyleGuideDocumentService, GitHubStyleGuideDocumentService>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "4dotnet-csharp-style-guide",
            Version = "1.0.0"
        };
    })
    .WithStdioServerTransport()
    .WithTools<StyleGuideTools>();

await builder.Build().RunAsync();
