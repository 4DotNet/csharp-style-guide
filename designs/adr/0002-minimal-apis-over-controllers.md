# ADR-0002: Use minimal APIs instead of controllers

- **Status:** Accepted
- **Date:** 2026-07-03

## Context

ASP.NET Core offers two ways to build HTTP APIs: the MVC **controller** model
(`Controller`/`ControllerBase` classes decorated with routing and action
attributes) and **minimal APIs** (endpoints registered directly against the
route builder with `MapGet`, `MapPost`, and friends).

Controllers carry a large amount of ceremony and implicit behavior: base
classes, attribute-driven routing and model binding, filters, and convention
scanning. They spread the definition of a single endpoint across attributes,
method signatures, and framework conventions, which makes endpoints harder to
read in isolation and encourages fat classes that group unrelated routes.

Minimal APIs express an endpoint as an explicit, self-contained registration.
Routing, parameter binding, dependencies, and the handler live in one place;
there is no base class and no hidden convention. They are lighter to test,
easier to reason about, faster to start, and align with the way we structure
the rest of our C# — small, explicit, and prescriptive. .NET 10 (see
[ADR-0001](0001-adopt-dotnet-10.md)) provides a mature minimal API surface,
including route groups (`MapGroup`), typed results, filters, and full OpenAPI
support, so controllers offer no capability we require.

## Decision

**All new HTTP endpoints are written as minimal API endpoints.**

- New endpoints MUST be registered as minimal API endpoints (`MapGet`,
  `MapPost`, `MapPut`, `MapDelete`, `MapPatch`, and `MapGroup` for grouping).
- Adding new controller endpoints is **strictly prohibited**. New classes
  deriving from `Controller` or `ControllerBase`, and new controller action
  methods, are a violation of this standard.
- Endpoints SHOULD be organized into small, cohesive registration units (for
  example, an endpoint mapping extension method per feature or resource) and
  grouped with `MapGroup` where a shared route prefix, tags, or filters apply.
- Handlers SHOULD keep logic out of the endpoint itself — inject dependencies
  and delegate to services — and return typed results (`TypedResults`) so the
  response shape is explicit and testable.

Existing controllers may remain until they are migrated, but no new
functionality is added to them: a change that would add a controller action is
instead implemented as a minimal API endpoint. Any exception requires a
separate, explicitly accepted ADR.

## Consequences

- **Positive:** Each endpoint is explicit and self-contained; less ceremony and
  no hidden MVC conventions; faster startup and lower overhead; endpoints are
  straightforward to unit-test and to document via OpenAPI; guidance elsewhere
  in this style guide can assume a single endpoint model.
- **Negative / cost:** Existing controller-based APIs require migration effort
  over time. Teams accustomed to the MVC controller model must learn the
  minimal API idioms (route groups, endpoint filters, typed results).
- **Enforcement:** Code review rejects new controllers and controller actions.
  Introducing a new `Controller`/`ControllerBase` type or action method should
  fail review, and ideally an analyzer or CI check flags new derivations from
  the controller base classes.
