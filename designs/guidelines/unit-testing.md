# Guideline: Unit testing

- **Status:** Accepted
- **Date:** 2026-07-03

## Summary

Every module **must** have a dedicated unit test project that covers **at least
80%** of its module library. Write unit tests with **xUnit**, using the
**`xunit.v3`** NuGet package. **Moq** and **Bogus.NET** are approved for mocking
and fake-data generation. **Do not use FluentAssertions** — it ships under a
commercial license.

## A dedicated test project per module

Each module **must** have its own dedicated unit test project. Name it after the
module library it exercises, with a `.Tests` suffix:

| Module library                 | Test project                         |
| ------------------------------ | ------------------------------------ |
| `FourDotnet.Webshop.Cart`      | `FourDotnet.Webshop.Cart.Tests`      |
| `FourDotnet.Webshop.Ordering`  | `FourDotnet.Webshop.Ordering.Tests`  |

- One test project per module — do **not** share a single test project across
  multiple modules, and do **not** fold a module's tests into another module's
  test project.
- The test project references only the module it tests (plus its transitive
  dependencies); keep the one-to-one mapping between a module and its tests clear.

Test projects live in a **central `Tests` folder** under `src` — **not** inside
the module folder. This keeps the module folders focused on shippable code while
still keeping the one-to-one module-to-test mapping obvious through the project
name:

```
src/
  Cart/
    FourDotnet.Webshop.Cart/
      FourDotnet.Webshop.Cart.csproj
    FourDotnet.Webshop.Cart.Abstractions/
      FourDotnet.Webshop.Cart.Abstractions.csproj
  Tests/
    FourDotnet.Webshop.Cart.Tests/
      FourDotnet.Webshop.Cart.Tests.csproj
    FourDotnet.Webshop.Ordering.Tests/
      FourDotnet.Webshop.Ordering.Tests.csproj
```

## Code coverage: at least 80% of the module library

Each module's test project **must** cover **at least 80%** of the code in its
module library — the project that holds the module's behaviour (for example
`FourDotnet.Webshop.Cart`). This is the number that gates the module.

Coverage of a module's **abstractions** project (for example
`FourDotnet.Webshop.Cart.Abstractions`) is **less relevant** and is **not**
counted toward the 80% threshold. Abstractions projects contain contracts —
interfaces, DTOs, records and other declarations with little or no behaviour — so
driving their line coverage adds little value. Focus coverage effort on the module
library where the logic actually lives.

> Measure coverage of the module library specifically, not of the whole solution.
> A solution-wide percentage can hide an under-tested module behind well-covered
> ones and lets low-value abstractions coverage inflate the number.

## Test framework: xUnit (v3)

xUnit is the recommended unit-testing framework for all .NET projects.

**Use the `xunit.v3` NuGet package.** This is critical:

- ✅ Reference **`xunit.v3`** — the current, maintained version.
- ❌ Do **not** reference the older `xunit` package (the v2 line) or its split
  packages such as `xunit.core` / `xunit.abstractions`. These are outdated and
  must not be used for new tests.

A test project's package reference should look like this:

```xml
<ItemGroup>
  <PackageReference Include="xunit.v3" Version="*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="*" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="*" />
</ItemGroup>
```

> Pin to the specific current version rather than `*` in real projects; `*` above
> is shorthand for "the latest stable release."

## Mocking: Moq

**Moq** is approved for creating test doubles (mocks/stubs) of dependencies.

```csharp
var repository = new Mock<IOrderRepository>();
repository
    .Setup(r => r.GetById(It.IsAny<int>()))
    .Returns(new Order());
```

## Fake data: Bogus.NET

**Bogus.NET** (`Bogus`) is approved for generating realistic fake data. Prefer it
over hand-rolled random data or large literal fixtures.

```csharp
var faker = new Faker<Customer>()
    .RuleFor(c => c.Id, f => f.Random.Guid())
    .RuleFor(c => c.Name, f => f.Name.FullName())
    .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.Name));

var customer = faker.Generate();
```

## Assertions: use .NET-native xUnit asserts

**Write assertions with the .NET-native xUnit `Assert` API.** It is the default
and expected assertion style across our projects.

**FluentAssertions is not allowed.** As of version 8 it is distributed under a
**commercial license**, which makes it unsuitable as a default dependency across
our projects.

Write assertions with xUnit's **built-in, .NET-native `Assert` API**. It ships
with the test framework, has no extra licensing or dependency cost, and is more
than expressive enough for the vast majority of tests:

```csharp
Assert.Equal(expected, actual);
Assert.True(order.IsValid);
Assert.Throws<InvalidOperationException>(() => service.Process(order));
Assert.Contains(item, collection);
```

The native `Assert` API covers equality, boolean conditions, exceptions,
collections, ranges, types and null checks. Reach for these first — do **not**
add a separate fluent-assertion library. Plain xUnit assertions are the default
and the expectation across our projects.

## Rationale

- A **dedicated test project per module** keeps the modular boundary intact: tests
  live and version alongside the module they cover, and coverage can be measured
  and enforced per module rather than blurred across the solution.
- The **80% module-library threshold** sets a concrete, enforceable bar for the
  code that carries behaviour, while excluding **abstractions** projects keeps the
  metric honest — contracts have little logic to exercise, so counting them would
  only let low-value coverage inflate the number.
- **`xunit.v3`** is the actively maintained line; older `xunit` v2 packages are
  outdated and should not seed new test projects.
- **Moq** and **Bogus.NET** keep tests focused and readable by removing dependency
  and fixture boilerplate.
- **.NET-native xUnit asserts** ship with the framework, add no licensing or
  dependency cost, and are expressive enough for nearly every test — so they are
  the default. **FluentAssertions** moved to a **commercial license**, introducing
  licensing cost and risk; avoiding it keeps our test dependencies free and
  unambiguous.
