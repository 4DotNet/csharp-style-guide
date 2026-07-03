# ADR-0008: Adopt Aspire for orchestration and service integration

- **Status:** Accepted
- **Date:** 2026-07-03

## Context

Our solutions are composed of multiple projects — modules, API hosts, and
backing services such as databases, caches, and message brokers (see
[ADR-0004](0004-modular-monolith-or-microservices-structure.md)). Wiring these
together for local development and deployment by hand leads to inconsistent
configuration, ad-hoc connection strings, and bespoke service discovery.

**Aspire** (formerly .NET Aspire) is Microsoft's opinionated stack for building
observable, production-ready distributed applications. It provides an app model
for orchestrating projects and their dependencies, consistent service discovery
and configuration, built-in telemetry, and a set of **integrations** for common
services — each of which brings a properly configured client library. Adopting
Aspire gives us one standard way to compose and run our systems.

## Decision

**Use Aspire for orchestration and service integration in all solutions.**

### Aspire projects live in an `Aspire` folder

Aspire projects (the app host and the service defaults) live in a folder called
**`Aspire`** under `src`:

```
src/
  Aspire/
    ...AppHost/
    ...ServiceDefaults/
```

This keeps the orchestration concerns in one predictable place, alongside the
modules, shared libraries, and API projects that also live under `src`.

### Always use the Aspire integration's client library

When integrating a service (a database, cache, message broker, storage account,
and so on), **always use the client library that comes with the corresponding
Aspire integration.** Add the service to the app host through its Aspire
integration and consume it in the module/API through the client library that
integration provides.

Do **not** hand-roll a client, bring an alternative client library, or configure
a raw connection when an Aspire integration exists for that service. Using the
Aspire-provided client is what gives us the consistent configuration, service
discovery, health checks, and telemetry that are the point of adopting Aspire.

If no Aspire integration exists for a required service, that gap must be handled
explicitly (and, if it establishes a pattern, captured in a separate ADR).

## Consequences

- **Positive:** One standard, opinionated way to orchestrate projects and their
  dependencies; consistent configuration, service discovery, health checks, and
  telemetry out of the box; correctly configured clients for integrated services
  without bespoke wiring; a predictable `src/Aspire` location across repositories.
- **Negative / cost:** A dependency on the Aspire stack and its conventions, and
  a learning curve for the app model. Services without an Aspire integration
  require explicit, one-off handling.
- **Enforcement:** Code review rejects solutions that orchestrate services
  outside Aspire when an integration exists, integrations consumed through a
  non-Aspire client library, and Aspire projects placed outside the
  `src/Aspire` folder.
