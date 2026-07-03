# ADR-0006: Organize code into feature slices

- **Status:** Accepted
- **Date:** 2026-07-03

## Context

Code can be organized by technical layer (all commands together, all handlers
together, all validators together) or by **feature** (everything for one
operation kept in one place). Layer-based organization scatters a single
operation across many folders and forces you to jump between them to understand
or change one behavior.

We organize by **feature slice**. Every endpoint maps to one feature, and
everything that feature needs lives together. This keeps a change to one
operation local, makes features easy to find, and pairs naturally with our CQRS
approach ([ADR-0005](0005-cqrs-pattern.md)) where each operation is a single
command or query with its handler.

## Decision

**Every endpoint is a feature, and features are organized strictly as described
below. This structure is required and has no exceptions.**

### One feature per endpoint

Each endpoint results in a **feature** named after the operation — for example
`CreateUser`, `UpdateUser`, `DeleteUser`, `GetUser`.

### Namespace structure

Features are **always** organized in the **`Features` namespace of the module**
they belong to (see modules in
[ADR-0004](0004-modular-monolith-or-microservices-structure.md)). Inside
`Features`, each feature has **its own namespace named after the feature**. That
feature namespace contains:

- The **command** or the **query** (`CreateUserCommand`, `GetUserQuery`).
- The **handler** for that command or query (`CreateUserCommandHandler`,
  `GetUserQueryHandler`).

Given the module `FourDotnet.Webshop.Cart`, the structure is:

```
FourDotnet.Webshop.Cart/
  Features/
    CreateUser/
      CreateUserCommand.cs          // FourDotnet.Webshop.Cart.Features.CreateUser
      CreateUserCommandHandler.cs
    UpdateUser/
      UpdateUserCommand.cs          // FourDotnet.Webshop.Cart.Features.UpdateUser
      UpdateUserCommandHandler.cs
    GetUser/
      GetUserQuery.cs               // FourDotnet.Webshop.Cart.Features.GetUser
      GetUserQueryHandler.cs
```

The namespace of every type in a feature is
`{Module}.Features.{FeatureName}` — for example
`FourDotnet.Webshop.Cart.Features.CreateUser`. Any other type the feature owns
(for example a feature-specific response object) lives in the same feature
namespace.

### Strictly required

This organization is **mandatory**. Placing a command/query or its handler
outside its module's `Features.{FeatureName}` namespace, grouping types by
technical layer, or combining multiple features into one namespace are
violations of this standard.

## Consequences

- **Positive:** Everything for one operation lives together, so a change stays
  local and features are trivial to locate by name; the structure is uniform
  across every module; and it aligns directly with the one-operation-per-handler
  CQRS model.
- **Negative / cost:** Many small folders/namespaces — one per endpoint — rather
  than a few large ones. Types genuinely shared across features must be lifted
  deliberately (to the module or a shared library) rather than left in a feature
  slice.
- **Enforcement:** Code review rejects commands, queries, or handlers that are
  not in their module's `Features.{FeatureName}` namespace, and any
  layer-based grouping of these types.
