# ADR-0009: Adopt OpenTelemetry for full observability

- **Status:** Accepted
- **Date:** 2026-07-07

## Context

Our systems are distributed by construction — modules, API hosts, and backing
services such as databases, caches, and message brokers (see
[ADR-0004](0004-modular-monolith-or-microservices-structure.md)). When something
is slow or wrong, we need to see **exactly** what happened: which request, which
handler, which downstream call, how long each took, and how often it happens.
Sprinkling ad-hoc logging is not enough — it is inconsistent, unstructured, and
cannot be correlated across process boundaries.

**OpenTelemetry (OTEL)** is the vendor-neutral standard for traces, metrics, and
logs. It gives us one instrumentation model that flows through the whole system,
correlates a single logical operation across every project and service it
touches, and exports to any backend. [Aspire](0008-adopt-aspire.md) already wires
OTEL end-to-end and its dashboard is built on it, so observability is not an
add-on for us — it is a first-class requirement.

We do not treat instrumentation as optional plumbing bolted on after the fact.
Every request that enters the system, and in particular every **command and
query handler** (see [ADR-0005](0005-cqrs-pattern.md)), is a unit of work we want
to see, time, and count.

## Decision

**Instrument every solution with OpenTelemetry to its fullest, and configure it
in one place.** Traces and metrics are required, not optional; command and query
handlers must be richly instrumented.

### Configure OTEL centrally — never per project by hand

OpenTelemetry is configured **once** and shared by every project. It is never
configured ad hoc inside individual modules or API hosts.

- **If the solution uses Aspire** (which it should — see
  [ADR-0008](0008-adopt-aspire.md)), configure OpenTelemetry through the **Aspire
  `ServiceDefaults`** project. Every project calls `AddServiceDefaults()` (which
  in turn calls the `ConfigureOpenTelemetry` plumbing that ships with the Service
  Defaults template) so tracing, metrics, and the OTLP exporter are wired
  identically everywhere. Extend that single `ServiceDefaults` project when you
  need extra instrumentation; do not reconfigure OTEL in a module or host.
- **If the solution does not use Aspire**, provide **one central extension
  method** — for example `AddObservability()` in the `Shared` project (see the
  `Shared` folder in
  [ADR-0004](0004-modular-monolith-or-microservices-structure.md)) — that every
  project calls to get identical OpenTelemetry configuration. The configuration
  lives in exactly one place; projects opt in with a single call.

Either way there is **one** OTEL setup for the whole solution, and every project
uses it.

### What the central configuration must set up

The shared configuration (Service Defaults or the central extension method) must
enable, at minimum:

- **Resource attributes** — service name, version, and environment on every
  signal so telemetry is attributable to a specific service and build.
- **Tracing** with ASP.NET Core, `HttpClient`, and the relevant client
  instrumentations (database, cache, messaging — use the instrumentation that
  ships with each Aspire integration's client library where applicable, per
  [ADR-0008](0008-adopt-aspire.md)).
- **Metrics** with ASP.NET Core, `HttpClient`, and runtime instrumentation.
- **Our own `ActivitySource` and `Meter`** — a single well-known
  `ActivitySource` name and `Meter` name per service (or per module) that our
  handlers use, registered so they are collected and exported.
- **OTLP export** — signals are exported over OTLP (the Aspire dashboard / a
  collector consumes them). Logs go through the OpenTelemetry logging provider so
  they carry trace correlation.

### Command and query handlers must be full of OTEL

Handlers are the heart of a request (see [ADR-0005](0005-cqrs-pattern.md)) and
must be **richly instrumented**. Every command handler and every query handler:

- **Starts an activity (span)** for the work it performs, from the shared
  `ActivitySource`, named after the operation (for example
  `CreateCart` / `GetCartById`). Downstream calls the handler makes become
  child spans automatically, so a single command or query yields a complete
  trace.
- **Tags the span with meaningful attributes** — the identifiers and inputs that
  matter for diagnosis (entity id, tenant, item count, and so on). Never put
  secrets or personal data on a span.
- **Records the outcome** — set the activity status to error and call
  `AddException` on failure so failed operations are visible and searchable;
  successful operations complete with an OK/unset status.
- **Emits metrics** where they carry signal — a **counter** for how often the
  operation runs (and how often it fails), and a **histogram** for its duration
  or for domain-meaningful measures (items added, bytes processed). Use the
  shared `Meter`, with low-cardinality tags (operation name, outcome) so the
  metrics stay aggregatable.

Because handlers all derive from the shared CQRS base classes
([ADR-0005](0005-cqrs-pattern.md)), put the common instrumentation plumbing
(starting the activity, timing the operation, recording success/failure counters)
in **that base class** so every handler is instrumented consistently and each
concrete handler only adds the attributes and domain metrics specific to it.

### Instrumentation is not optional

A handler with no tracing or metrics is incomplete. New command/query handlers,
new integrations, and new background work must be instrumented as part of the
change — the same way they must be tested (see
[the unit-testing guideline](../guidelines/unit-testing.md)). Do not add a
separate, competing observability stack alongside OpenTelemetry.

## Consequences

- **Positive:** Every logical operation is traceable end-to-end across projects
  and services; handlers are timed and counted, so slow and failing operations
  are immediately visible; observability is configured once and identical
  everywhere; and the setup works out of the box with the Aspire dashboard and
  any OTLP backend.
- **Negative / cost:** Instrumentation is real work per handler, and the shared
  base-class plumbing plus the central configuration must be maintained. Care is
  needed to keep metric tag cardinality low and to keep secrets/PII off spans.
- **Enforcement:** Code review rejects OpenTelemetry configured per project
  instead of through the Aspire `ServiceDefaults` (or the single central
  extension method when Aspire is not used), command/query handlers that emit no
  traces or metrics, use of a competing observability stack, and spans carrying
  secrets or personal data.
