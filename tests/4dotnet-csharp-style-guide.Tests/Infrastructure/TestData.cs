namespace FourDotNet.CSharpStyleGuide.Tests.Infrastructure;

/// <summary>
/// The canned manifest and document bodies served to the service under test. Designed
/// to exercise every filtering, ranking and relationship path:
/// <list type="bullet">
///   <item>adr-0001 — active, carries rules and shares tags with others.</item>
///   <item>adr-0002 — active status but superseded by adr-0003, so inactive.</item>
///   <item>adr-0003 — active, supersedes adr-0002.</item>
///   <item>adr-0004 — deprecated, so inactive.</item>
///   <item>guideline-unit-testing — active guideline with a rule.</item>
/// </list>
/// </summary>
internal static class TestData
{
    public const string IndexJson = """
    {
      "version": 1,
      "documents": [
        {
          "id": "adr-0001",
          "type": "adr",
          "title": "Pragmatic DDD domain models",
          "path": "adr/0001-active.md",
          "status": "accepted",
          "date": "2026-01-01",
          "summary": "How we model domain entities in web-api projects.",
          "tags": ["testing", "web-api"],
          "rules": [
            {
              "id": "adr-0001-r1",
              "rule": "Endpoints must validate their input.",
              "severity": "required",
              "appliesTo": "web-api"
            },
            {
              "id": "adr-0001-r2",
              "rule": "Prefer records for immutable value objects.",
              "severity": "recommended",
              "appliesTo": "tests"
            }
          ]
        },
        {
          "id": "adr-0002",
          "type": "adr",
          "title": "Old approach",
          "path": "adr/0002-superseded.md",
          "status": "accepted",
          "date": "2025-01-01",
          "summary": "The previous, now replaced approach.",
          "tags": ["web-api"],
          "rules": []
        },
        {
          "id": "adr-0003",
          "type": "adr",
          "title": "New architecture approach",
          "path": "adr/0003-supersedes.md",
          "status": "accepted",
          "date": "2026-02-01",
          "summary": "The current architecture guidance.",
          "tags": ["web-api", "architecture"],
          "supersedes": "adr-0002",
          "rules": []
        },
        {
          "id": "adr-0004",
          "type": "adr",
          "title": "Legacy logging",
          "path": "adr/0004-deprecated.md",
          "status": "deprecated",
          "date": "2024-01-01",
          "summary": "Deprecated logging guidance.",
          "tags": ["legacy"],
          "rules": []
        },
        {
          "id": "guideline-unit-testing",
          "type": "guideline",
          "title": "Unit testing guideline",
          "path": "guidelines/unit-testing.md",
          "status": "accepted",
          "date": "2026-03-01",
          "summary": "How to write unit tests for handlers.",
          "tags": ["testing", "tests"],
          "rules": [
            {
              "id": "guideline-unit-testing-r1",
              "rule": "Every handler must have a unit test.",
              "severity": "required",
              "appliesTo": "tests"
            }
          ]
        }
      ]
    }
    """;

    public const string ActiveAdrBody = """
    # Pragmatic DDD domain models

    Domain entities should encapsulate their invariants.
    The word aggregate appears only in this document body.
    """;

    public const string SupersededAdrBody = "# Old approach\n\nThis has been replaced.";

    public const string SupersedesAdrBody = "# New architecture approach\n\nUse feature slices.";

    public const string DeprecatedAdrBody = "# Legacy logging\n\nDo not use this anymore.";

    public const string GuidelineBody = """
    # Unit testing guideline

    Write one unit test per handler.
    Use xUnit and keep tests fast.
    """;
}
