# ADR-0004: Modular Monolith or Microservices solution structure

- **Status:** Accepted
- **Date:** 2026-07-03

## Context

Our systems are built around **bounded contexts**. To keep those contexts
isolated, independently evolvable, and free of accidental coupling, we need a
single, prescribed way to lay out a solution. Two deployment styles are
supported — a **Modular Monolith** and **Microservices** — and both share the
same module structure so that a system can start as a modular monolith and split
into microservices (or the reverse) without reshaping every project.

The examples below assume the company is **FourDotnet**, the product is
**Webshop**, and a module is **Cart**. Substitute your own company, product, and
module names, but keep the structure identical.

## Decision

**Every solution follows the Modular Monolith or the Microservices structure
below — strictly.** No other layout is permitted.

### One module per bounded context

Every bounded context is represented by a **single module**. A module always
consists of **two projects**:

- **The module project** — the implementation of the bounded context.
- **The abstractions project** — the module's public contract (interfaces,
  DTOs, integration event definitions) that other modules are allowed to
  reference. Other modules depend on the abstractions project **only**, never on
  the module project.

The module lives in a folder named after the module, containing both projects:

```
Cart/
  FourDotnet.Webshop.Cart/
    FourDotnet.Webshop.Cart.csproj
  FourDotnet.Webshop.Cart.Abstractions/
    FourDotnet.Webshop.Cart.Abstractions.csproj
```

### API projects differ by deployment style

**Microservices** — each module is deployed independently, so **each module has
its own API project** inside the module folder:

```
Cart/
  FourDotnet.Webshop.Cart/
    FourDotnet.Webshop.Cart.csproj
  FourDotnet.Webshop.Cart.Abstractions/
    FourDotnet.Webshop.Cart.Abstractions.csproj
  FourDotnet.Webshop.Cart.Api/
    FourDotnet.Webshop.Cart.Api.csproj
```

**Modular Monolith** — all modules are deployed together, so the **solution root
contains a single API project** that hosts every module. Module folders contain
only the module and abstractions projects (no per-module API):

```
FourDotnet.Webshop.Api/
  FourDotnet.Webshop.Api.csproj
```

### Shared code

Every solution needs code shared across modules — DDD base classes (see
[ADR-0003](0003-pragmatic-ddd-domain-models.md)), CQRS handling, integration
events, and similar cross-cutting concerns. This lives in a **`Shared` folder**
containing one or more shared code libraries, for example:

```
Shared/
  FourDotnet.Webshop.Shared/
    FourDotnet.Webshop.Shared.csproj
  FourDotnet.Webshop.Core/
    FourDotnet.Webshop.Core.csproj
```

Shared libraries must contain only genuinely cross-cutting plumbing. They must
not become a dumping ground for module-specific logic, which would recouple the
bounded contexts.

### Everything lives under `src`

The entire solution is organized in a folder named **`src`** at the repository
root.

### Full example layouts

**Microservices:**

```
src/
  Cart/
    FourDotnet.Webshop.Cart/
      FourDotnet.Webshop.Cart.csproj
    FourDotnet.Webshop.Cart.Abstractions/
      FourDotnet.Webshop.Cart.Abstractions.csproj
    FourDotnet.Webshop.Cart.Api/
      FourDotnet.Webshop.Cart.Api.csproj
  Shared/
    FourDotnet.Webshop.Shared/
      FourDotnet.Webshop.Shared.csproj
    FourDotnet.Webshop.Core/
      FourDotnet.Webshop.Core.csproj
```

**Modular Monolith:**

```
src/
  Cart/
    FourDotnet.Webshop.Cart/
      FourDotnet.Webshop.Cart.csproj
    FourDotnet.Webshop.Cart.Abstractions/
      FourDotnet.Webshop.Cart.Abstractions.csproj
  Shared/
    FourDotnet.Webshop.Shared/
      FourDotnet.Webshop.Shared.csproj
    FourDotnet.Webshop.Core/
      FourDotnet.Webshop.Core.csproj
  FourDotnet.Webshop.Api/
    FourDotnet.Webshop.Api.csproj
```

## Consequences

- **Positive:** Bounded contexts are isolated behind abstractions projects,
  which keeps coupling explicit and one-directional; the layout is identical
  across both deployment styles, so moving between a modular monolith and
  microservices is a mechanical change rather than a rewrite; a predictable,
  uniform structure makes every repository easy to navigate.
- **Negative / cost:** More projects per module (at least two, three for
  microservices) than an ad-hoc layout. Discipline is required to keep the
  abstractions project as the only cross-module dependency and to keep shared
  libraries free of module-specific code.
- **Enforcement:** Code review rejects solutions that deviate from this layout —
  modules without an abstractions project, cross-module references to a module
  project instead of its abstractions, per-module APIs in a modular monolith (or
  a single root API in a microservices solution), shared libraries carrying
  module logic, or a solution not rooted under `src`.
