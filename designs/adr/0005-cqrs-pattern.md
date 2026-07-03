# ADR-0005: Command Query Responsibility Segregation (CQRS)

- **Status:** Accepted
- **Date:** 2026-07-03

## Context

We separate operations that **change** state from operations that **read** state.
This separation — CQRS — keeps write logic and read logic independent, makes each
handler small and single-purpose, and gives every request an explicit,
testable entry point.

A common shortcut is to reach for an external mediator library (for example
MediatR) to wire requests to handlers. We deliberately **do not** do this. The
pattern is small enough to own ourselves, and owning it keeps us free of a
third-party dependency, its versioning, and — increasingly — its licensing.

## Decision

**Use CQRS for all read and write operations, implemented with our own base
classes. External libraries for this pattern are strictly prohibited.**

### Shared base classes

A **shared project** (see the `Shared` folder in
[ADR-0004](0004-modular-monolith-or-microservices-structure.md)) contains the
base abstractions for the pattern:

- **Command** — represents an intent to change state.
- **Query** — represents an intent to read state.
- **Command handler** — handles exactly one command type.
- **Query handler** — handles exactly one query type.

These base classes are hand-written and live in the shared project so every
module implements the pattern the same way without repeating the plumbing.

- A **query always has a response object** — reading exists to return data.
- A **command's response object is optional** — a command may return a result
  (for example the id of a created entity) or nothing at all.

### Request flow

1. Every HTTP request receives a plain **Data Transfer Object** — a C# `record`
   with no behavior.
2. The HTTP handler (a minimal API endpoint — see
   [ADR-0002](0002-minimal-apis-over-controllers.md)) **converts the DTO into a
   command or a query**.
3. The handler resolves the corresponding command handler or query handler via
   **dependency injection**.
4. That handler executes the request and returns its response (always for a
   query, optionally for a command).

The endpoint itself contains no business logic; it only maps the DTO to a
command/query, dispatches to the injected handler, and shapes the result into an
HTTP response.

### No external libraries

**Do not** introduce MediatR or any other mediator/CQRS framework. Commands,
queries, handlers, their base classes, and their registration/dispatch are all
implemented in our own code. This rule is strict and has no exception without a
separate, explicitly accepted ADR.

## Consequences

- **Positive:** Reads and writes are cleanly separated; every operation is a
  small, single-responsibility handler that is trivial to unit-test; the request
  entry point is explicit; and we carry no third-party dependency, versioning
  risk, or licensing constraint for the pattern.
- **Negative / cost:** We maintain the base classes and the handler
  registration/dispatch ourselves, and there is some boilerplate per operation
  (a DTO, a command/query, and a handler).
- **Enforcement:** Code review rejects any use of an external mediator/CQRS
  library, endpoints that contain business logic instead of dispatching to a
  handler, handlers that do not derive from the shared base classes, and queries
  without a response object.
