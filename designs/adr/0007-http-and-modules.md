# ADR-0007: HTTP endpoints live in the module, not the API project

- **Status:** Accepted
- **Date:** 2026-07-03

## Context

A module ([ADR-0004](0004-modular-monolith-or-microservices-structure.md)) is a
bounded context that we want to be **portable**: the same module should slot into
a modular monolith or run as a microservice without change. If endpoint mappings
and handler registrations live in the API project, the module is not
self-contained — the host has to know the module's internals, and moving the
module means rewriting host code.

To keep modules portable, a module must own **everything** it needs to be hosted:
its handler registrations and its HTTP endpoint mappings. The API project becomes
a thin host that simply composes modules.

## Decision

**All HTTP endpoint mappings are organized in the module library. API projects
contain no endpoint mappings.**

### The API project is a thin host

The API project (the per-module API for microservices, or the single root API for
a modular monolith — see [ADR-0004](0004-modular-monolith-or-microservices-structure.md))
**does not contain any endpoint mappings**. It only composes modules by calling
their extension methods.

### Every module exposes two extension methods

Each module — whether hosted in a monolith or as a microservice — provides
exactly two extension methods:

1. **One extension method on the host/application builder that registers all of
   the module's features** via dependency injection (its command and query
   handlers — see [ADR-0005](0005-cqrs-pattern.md) and
   [ADR-0006](0006-feature-slices.md)):

   ```csharp
   builder.AddCartModule();   // AddUsersModule(), AddCartModule(), ...
   ```

2. **One extension method on the web application that maps all of the module's
   HTTP endpoints:**

   ```csharp
   app.MapCartEndpoints();     // MapUsersEndpoints(), MapCartEndpoints(), ...
   ```

A host wires up a module with just these two calls, so adding or removing a
module from a host is trivial and the module stays portable.

### The `Endpoints` namespace

Every module contains a namespace called **`Endpoints`**. This namespace holds a
file — or multiple files — where the endpoints are configured (the minimal API
mappings from [ADR-0002](0002-minimal-apis-over-controllers.md), each dispatching
to its feature handler). The public `Map{Module}Endpoints()` extension method is
defined here and delegates to those configurations.

**Use `MapGroup` as much as possible** to share a route prefix, tags, filters,
and metadata across related endpoints — but apply it **pragmatically, not
dogmatically**. Group where it genuinely reduces repetition; do not force a group
where it adds ceremony without value.

## Consequences

- **Positive:** Modules are fully self-contained and portable — the same module
  runs in a monolith or a microservice with no host changes; hosts are thin and
  compose modules with two calls each; endpoint configuration lives next to the
  module it belongs to.
- **Negative / cost:** Each module must maintain its two extension methods and
  its `Endpoints` namespace. The host cannot "see" all routes in one project;
  they are distributed across modules by design.
- **Enforcement:** Code review rejects any endpoint mapping in an API project,
  modules missing either extension method, and endpoint configurations placed
  outside the module's `Endpoints` namespace.
