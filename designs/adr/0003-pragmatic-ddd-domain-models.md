# ADR-0003: Pragmatic Domain-Driven Design with rich domain models

- **Status:** Accepted
- **Date:** 2026-07-03

## Context

We want the benefits of Domain-Driven Design — a domain model that protects its
own invariants and expresses intent — without the full ceremony of a textbook
DDD implementation. Anemic models (public getters and setters, validation
scattered across services) let invalid state spread through the system and push
business rules into places where they are easy to forget.

We take a **pragmatic** stance: entities are represented by rich domain models
that own their data and their validation, expose intent-revealing operations
instead of open setters, and track their own lifecycle state so persistence and
change tracking can be reasoned about consistently.

## Decision

### Entities are domain models

Every entity that lives in the system is represented by a **domain model**. The
domain model owns its data and enforces its own invariants.

### Encapsulated properties

Properties expose a **public getter and a private setter**:

```csharp
public string Name { get; private set; }
```

State is never mutated directly from outside the model.

### Change through intent-revealing functions

When a property needs to change, a **public function** performs the change. That
function contains **all** validation for the value and validates the new value
**before** assigning it:

```csharp
public void SetName(string value)
{
    // ALL validation for Name lives here, and runs before the value is set.
    if (string.IsNullOrWhiteSpace(value))
        throw new DomainValidationException("Name is required.");
    if (value.Length > 100)
        throw new DomainValidationException("Name must be 100 characters or fewer.");

    SetName(...); // apply the change through the base-class mechanism (see State)
}
```

### Value objects for multi-value changes

When **multiple values must be set at once**, introduce a **value object**. The
value object is **thoroughly validated on construction** so it cannot exist in
an invalid state, and the domain model accepts the already-valid value object as
a unit:

```csharp
public sealed record Address
{
    public Address(string street, string city, string postalCode)
    {
        // Cross-field and per-field validation happens here, before the
        // value object can be accepted by any domain model.
        ...
    }
}

public void SetAddress(Address address) { ... }
```

### Lifecycle state

A domain model **maintains a state**. The allowed states are:

| State        | Meaning                                                                                              |
| ------------ | ---------------------------------------------------------------------------------------------------- |
| **New**      | The model was newly created in memory and does not yet exist in the data store.                      |
| **Pristine** | The model was materialized (read) from the data store and has not been changed since.                |
| **Touched**  | A `SetX()` function was called but the value did not actually change (e.g. `Name` is `"Eduard"` and `SetName("Eduard")` is called). |
| **Modified** | A `SetX()` function was called that actually changed a value (e.g. `SetName("Peter")`).              |
| **Deleted**  | The model's `Delete()` function was called.                                                          |

**Initial state may only be `New` or `Pristine`** — nothing else. A model
constructed in code starts as `New`; a model rehydrated from a data store starts
as `Pristine`. The state then flows as `SetX()` and `Delete()` operations are
applied. Because a `SetX()` function distinguishes a no-op change (`Touched`)
from a real change (`Modified`), the state accurately reflects whether the model
needs to be persisted.

### Base classes live in a shared project

The **plumbing** for domain models — maintaining lifecycle state, enforcing a
correct state flow (e.g. `New`/`Pristine` initial states, transitioning to
`Touched`/`Modified`/`Deleted`), and the helpers that `SetX()` functions call to
apply a change — lives in **domain model base classes**. These base classes are
stored in a **shared project** (for example `*.Shared` or `*.Core`) so that the
actual domain model implementations contain only their own properties,
validation, and operations — **not** the required plumbing.

## Consequences

- **Positive:** Invariants are enforced in one place (the model), validation
  cannot be bypassed, intent is explicit through named operations, and lifecycle
  state gives persistence and change tracking a reliable signal for what needs
  saving. Shared base classes keep individual models free of boilerplate.
- **Negative / cost:** More code per property than an anemic setter, and a
  learning curve around value objects and state transitions. The shared base
  classes are a piece of infrastructure that must be maintained and understood.
- **Enforcement:** Code review rejects entities exposing public setters or
  performing validation outside the model. Multi-value changes without a value
  object, and domain models that re-implement plumbing instead of deriving from
  the shared base classes, are violations of this standard.
