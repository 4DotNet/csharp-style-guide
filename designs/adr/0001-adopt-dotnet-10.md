# ADR-0001: Adopt .NET 10 as the standard for all .NET projects

- **Status:** Accepted
- **Date:** 2026-07-03

## Context

We maintain and create a range of .NET projects across teams. Running on a mix
of .NET versions creates avoidable cost: divergent language feature availability,
inconsistent runtime behavior, repeated per-project upgrade decisions, and a
larger surface area to keep patched and secure. Older versions also fall out of
Microsoft's support window, leaving projects without security fixes.

.NET 10 is the current Long-Term Support (LTS) release. Standardizing on a single,
supported LTS version gives us a predictable baseline for language features,
tooling, and runtime behavior, and lets our guidance assume one target rather than
hedging across versions.

## Decision

**All .NET projects target .NET 10.**

- New projects are created targeting .NET 10 (`net10.0`).
- Existing projects are migrated to .NET 10.
- Projects that reference a lower version of .NET are **not allowed**. A target
  framework below `net10.0` is a violation of this standard and must be brought up
  to .NET 10 before the project is considered compliant.

Multi-targeting that *includes* an older framework alongside `net10.0` is also
disallowed unless a separate, explicitly accepted ADR grants an exception (for
example, a library that must ship a `netstandard2.0` target for external
consumers). Absent such an ADR, `net10.0` is the sole target.

## Consequences

- **Positive:** One supported LTS baseline across all projects; the latest C#
  language features are uniformly available; a smaller matrix to build, test,
  patch, and secure; simpler, unhedged guidance in the rest of this style guide.
- **Negative / cost:** Existing projects on older frameworks require migration
  effort. Third-party dependencies that do not yet support .NET 10 may block a
  migration and must be resolved or replaced.
- **Enforcement:** Target framework should be verified in CI. Any project or PR
  introducing a target framework below `net10.0` should fail the check.
