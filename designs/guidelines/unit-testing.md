# Guideline: Unit testing

- **Status:** Accepted
- **Date:** 2026-07-03

## Summary

Write unit tests with **xUnit**, using the **`xunit.v3`** NuGet package. **Moq**
and **Bogus.NET** are approved for mocking and fake-data generation. **Do not use
FluentAssertions** — it ships under a commercial license.

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

## Assertions: do NOT use FluentAssertions

**FluentAssertions is not allowed.** As of version 8 it is distributed under a
**commercial license**, which makes it unsuitable as a default dependency across
our projects.

Use xUnit's built-in assertions instead:

```csharp
Assert.Equal(expected, actual);
Assert.True(order.IsValid);
Assert.Throws<InvalidOperationException>(() => service.Process(order));
Assert.Contains(item, collection);
```

If a more expressive, fluent assertion style is genuinely needed, use a
permissively licensed alternative (for example, **Shouldly**) rather than
FluentAssertions — but plain xUnit assertions are the default and are sufficient
for the vast majority of tests.

## Rationale

- **`xunit.v3`** is the actively maintained line; older `xunit` v2 packages are
  outdated and should not seed new test projects.
- **Moq** and **Bogus.NET** keep tests focused and readable by removing dependency
  and fixture boilerplate.
- **FluentAssertions** moved to a **commercial license**, introducing licensing
  cost and risk. Avoiding it keeps our test dependencies free and unambiguous.
