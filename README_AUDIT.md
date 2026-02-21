# 🧹 Technical Audit Report — Raftel Framework

> **Date:** February 2025  
> **Scope:** Full repository audit (Domain, Application, Infrastructure, API, Tests)  
> **Target:** .NET 10.0 / Clean Architecture / DDD framework

---

## 📋 Executive Summary

Raftel is a well-structured training/template framework for building .NET Core applications following Clean Architecture and DDD principles. The codebase demonstrates strong architectural foundations with clear layer separation, CQRS implementation, multitenancy support, and comprehensive testing infrastructure.

**Overall Assessment: Solid foundation with targeted improvement opportunities.**

### Strengths

- Clear Clean Architecture layer separation (Domain → Application → Infrastructure → API)
- Well-implemented CQRS pattern with command/query dispatchers and middleware pipeline
- Strong DDD foundations with aggregates, value objects, specifications, and typed IDs
- Railway-oriented error handling via `Result<T>` pattern
- Comprehensive test suite (~50+ test files, ~75-80% coverage)
- Feature-folder organization following domain-driven structure
- No external framework dependencies — pure .NET approach

### Key Areas for Improvement

- Domain encapsulation: public setters on domain entities violate DDD principles
- Multitenancy security: tenant ID bypass when tenant context is null
- API error handling: missing global exception handler and input validation
- Test gaps: User and Tenant domain entities lack direct unit tests
- Value object safety: implicit conversions can throw unhandled exceptions

---

## 🔍 Detailed Findings

### 1️⃣ Domain Layer

#### ✅ What's Working Well

| Aspect | Assessment |
|--------|-----------|
| Aggregate roots | Properly defined with factory methods (`Create()`) |
| Typed IDs | `TypedGuidId`, `PirateId`, `UserId`, etc. — strong type safety |
| Specification pattern | Clean implementation with `And`/`Or` composition |
| Validator framework | Fluent `EnsureThat()` rule registration |
| Result/Error pattern | Well-implemented railway-oriented error handling |
| Private constructors | Consistently enforced across aggregates |

#### ⚠️ Issues Found

| # | Severity | Issue | Location |
|---|----------|-------|----------|
| D1 | 🔴 High | **Public setters on domain entities** — `User.Email`, `User.Name`, `User.Surname`, `Tenant.Name`, `Tenant.Code`, `Tenant.Description`, `Permission.Description`, `Role.Description` all have `{ get; set; }` | `User.cs`, `Tenant.cs`, `Permission.cs`, `Role.cs` |
| D2 | 🟡 Medium | **Missing validation in User constructor** — `name` and `surname` can be empty strings; no null check on email | `User.cs` constructor |
| D3 | 🟡 Medium | **Unsafe implicit conversion in Email** — `implicit operator Email(string)` throws `InvalidOperationException` if email is invalid instead of returning a domain error | `Email.cs` |
| D4 | 🟡 Medium | **Double call in Code conversion** — `explicit operator Code(string)` calls `Create()` twice (once for check, once for value) | `Code.cs` |
| D5 | 🟡 Medium | **Unused domain error** — `TenantErrors.NameRequired` is defined but never used in validation | `TenantErrors.cs` |
| D6 | 🟢 Low | **Specification expression not cached** — `IsSatisfiedBy()` recompiles expression on every call | `Specification.cs` |
| D7 | 🟢 Low | **PermissionCollection.AddRange() loses errors** — only returns the first error, discards the rest | `PermissionCollection.cs` |

#### 📝 Suggested Refactoring

**D1 — Replace public setters with domain methods:**

```csharp
// Before (current)
public Email Email { get; set; }

// After (recommended)
public Email Email { get; private set; }

public Result ChangeName(string newName)
{
    if (string.IsNullOrWhiteSpace(newName))
        return Result.Failure(UserErrors.NameRequired);
    Name = newName;
    return Result.Success();
}
```

**D4 — Fix double call in Code conversion:**

```csharp
// Before (current)
public static explicit operator Code(string value) =>
    Create(value).IsSuccess ? Create(value).Value : throw new InvalidOperationException();

// After (recommended)
public static explicit operator Code(string value)
{
    var result = Create(value);
    return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.Error.Message);
}
```

---

### 2️⃣ Application Layer

#### ✅ What's Working Well

| Aspect | Assessment |
|--------|-----------|
| CQRS implementation | Clean command/query separation with dispatchers |
| Middleware pipeline | Extensible with global and per-command/query middlewares |
| Validation middleware | Automatic validation before handler execution |
| UnitOfWork middleware | Transaction safety on command execution |
| Permission authorization | Attribute-driven with `[RequiresPermission]` |
| Handler coverage | 100% of handlers have tests |

#### ⚠️ Issues Found

| # | Severity | Issue | Location |
|---|----------|-------|----------|
| A1 | 🔴 High | **Missing null check after repository lookup** — `GetByIdAsync()` can return null but result is used without check | `AssignRoleToUserCommandHandler.cs` |
| A2 | 🟡 Medium | **Incomplete command validation** — `RegisterUserCommandValidator` lacks password strength rules; `CreateTenantCommandValidator` doesn't validate Code format | Validators |
| A3 | 🟡 Medium | **Inconsistent dispatcher implementations** — `CommandDispatcher` uses primary constructor, `QueryDispatcher` uses traditional constructor; different CancellationToken handling | `CommandDispatcher.cs`, `QueryDispatcher.cs` |
| A4 | 🟡 Medium | **No handler-level exception handling** — If a service throws (not returns Result), exceptions propagate uncaught with no logging | All handlers |
| A5 | 🟢 Low | **Tight coupling in handlers** — Handlers directly depend on multiple repositories; domain services could abstract complex business operations | Multiple handlers |

#### 📝 Suggested Refactoring

**A1 — Add null check in AssignRoleToUserCommandHandler:**

```csharp
// Before (current)
var user = await userRepository.GetByIdAsync(userId, token);
var role = await roleRepository.GetByIdAsync(roleId, token);
// ← Missing null checks

// After (recommended)
var user = await userRepository.GetByIdAsync(userId, token);
if (user is null)
    return Result.Failure(UserErrors.NotFound);

var role = await roleRepository.GetByIdAsync(roleId, token);
if (role is null)
    return Result.Failure(RoleErrors.NotFound);
```

---

### 3️⃣ Infrastructure Layer

#### ✅ What's Working Well

| Aspect | Assessment |
|--------|-----------|
| Generic DbContext | Abstract `RaftelDbContext<TDbContext>` with multitenancy built-in |
| Repository pattern | Clean `EfRepository` base with typed ID support |
| Entity configurations | Proper EF Core `IEntityTypeConfiguration` per entity |
| Audit interceptor | Automatic `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` |
| Data filters | Soft delete and tenant filters with enable/disable capability |
| Database providers | SQL Server and PostgreSQL support via `DatabaseProvider` |

#### ⚠️ Issues Found

| # | Severity | Issue | Location |
|---|----------|-------|----------|
| I1 | 🔴 High | **Tenant filter bypass when null** — If `CurrentTenantId == null`, the tenant filter is bypassed, exposing all tenants' data | `RaftelDbContext.cs` filter expression |
| I2 | 🔴 High | **Development certificates in all environments** — `AddDevelopmentEncryptionCertificate()` and `AddDevelopmentSigningCertificate()` hardcoded without environment check | `DependencyInjection.cs` |
| I3 | 🟡 Medium | **No tenant existence validation** — `TenantMiddleware` accepts any valid GUID as tenant ID without verifying the tenant exists in the database | `TenantMiddleware.cs` |
| I4 | 🟡 Medium | **Tenant interceptor only applies on INSERT** — `TenantInterceptor` sets TenantId only for `EntityState.Added`; no protection against tenant ID modification on UPDATE | `TenantInterceptor.cs` |
| I5 | 🟡 Medium | **Inconsistent null defaults for data filters** — Soft delete defaults to `false` (disabled) while tenant filter defaults to `true` (enabled) when `_dataFilter` is null | `RaftelDbContext.cs` |
| I6 | 🟡 Medium | **Hardcoded claim type strings** — `"permission"` and `"role"` claim types are hardcoded in multiple locations without a shared constant | `ClaimsPrincipalFactory.cs`, `CurrentHttpUser.cs` |
| I7 | 🟡 Medium | **Role assignment not tenant-aware** — `AuthenticationService.AssignRoleAsync()` uses `userManager.FindByEmailAsync()` which searches across all tenants | `AuthenticationService.cs` |
| I8 | 🟢 Low | **Multiple DbContext constructors** — Four constructor overloads make DI behavior unclear | `RaftelDbContext.cs` |
| I9 | 🟢 Low | **Missing soft delete on core entities** — User, Role, Permission entities don't use `.HasSoftDelete()` in their configurations | Entity configurations |

#### 📝 Suggested Refactoring

**I6 — Extract claim type constants:**

```csharp
// New file: ClaimTypes.cs
public static class RaftelClaimTypes
{
    public const string Permission = "permission";
    public const string Role = "role";
}
```

---

### 4️⃣ API Layer

#### ✅ What's Working Well

| Aspect | Assessment |
|--------|-----------|
| Auto-endpoint generation | Elegant reflection-based mapping from commands/queries to endpoints |
| OpenAPI integration | Automatic Swagger documentation |
| Minimal API approach | Clean, modern .NET endpoint registration |
| Middleware pipeline | Proper ordering (routing → tenant → auth → authorization) |

#### ⚠️ Issues Found

| # | Severity | Issue | Location |
|---|----------|-------|----------|
| P1 | 🔴 High | **No global exception handler** — Unhandled exceptions expose stack traces; no `UseExceptionHandler()` middleware | `Program.cs` (demo API) |
| P2 | 🔴 High | **Unhandled JSON parsing exceptions** — `ReadFromJsonAsync` failure throws `JsonException` (500) instead of returning 400 | `CommandEndpointMapper.cs` |
| P3 | 🟡 Medium | **Unsafe type conversions** — `Guid.Parse()`, `Enum.Parse()`, `Convert.ChangeType()` in query parameter binding can throw unhandled exceptions | `QueryEndpointMapper.cs` |
| P4 | 🟡 Medium | **Auto-anonymous endpoints** — Endpoints without `[RequiresPermission]` attribute default to `AllowAnonymous()` instead of requiring authentication | `EndpointRouteBuilderExtensions.cs` |
| P5 | 🟡 Medium | **Missing OpenAPI response schemas** — `WithOpenApi()` called without response status codes or error schemas | Endpoint mappers |
| P6 | 🟢 Low | **Nullable reference types disabled** — `<Nullable>disable</Nullable>` in API projects despite using null-forgiving operators (`!`) | `.csproj` files |

---

### 5️⃣ Testing

#### ✅ What's Working Well

| Aspect | Assessment |
|--------|-----------|
| Test framework | xUnit v3 with Shouldly assertions |
| Mocking | NSubstitute properly used for dependencies |
| Test naming | Descriptive names: `Validate_WhenEmailIsInvalid_ShouldReturnFailure` |
| AAA pattern | Consistently applied across all test files |
| Handler coverage | 8/8 handlers tested (100%) |
| Validator coverage | 2/2 validators tested (100%) |
| Integration tests | Testcontainers for database, functional API tests |
| Architecture tests | NetArchTest-based layer dependency enforcement |

#### ⚠️ Coverage Gaps

| # | Priority | Missing Tests | Suggested Location |
|---|----------|--------------|-------------------|
| T1 | 🔴 High | **User domain entity** — `AssignRole()`, `BindTo()`, factory methods | `tests/Raftel.Domain.Tests/Features/Users/UserTests.cs` |
| T2 | 🔴 High | **Tenant domain entity** — `Create()` factory method validation | `tests/Raftel.Domain.Tests/Features/Tenants/TenantTests.cs` |
| T3 | 🟡 Medium | **Email value object** — Format validation, edge cases, implicit conversion | `tests/Raftel.Domain.Tests/Features/Users/ValueObjects/EmailTests.cs` |
| T4 | 🟡 Medium | **Code value object** — Length validation, special characters | `tests/Raftel.Domain.Tests/ValueObjects/CodeTests.cs` |
| T5 | 🟡 Medium | **PermissionCollection** — Add, AddRange, duplicate handling | `tests/Raftel.Domain.Tests/Features/Authorization/PermissionCollectionTests.cs` |
| T6 | 🟡 Medium | **Middleware unit tests** — ValidationMiddleware, PermissionAuthorizationMiddleware in isolation | `tests/Raftel.Application.UnitTests/Middlewares/` |
| T7 | 🟡 Medium | **Null return scenarios** — AssignRoleToUserCommandHandler when user/role not found | Handler test files |
| T8 | 🟢 Low | **TenantInterceptor** — Tenant ID assignment on insert, update protection | `tests/Raftel.Infrastructure.Tests/` |
| T9 | 🟢 Low | **AuditPropertiesInterceptor** — Soft delete behavior, audit property population | `tests/Raftel.Infrastructure.Tests/` |

#### 📝 Suggested Test Examples

**T1 — User domain entity tests:**

```csharp
public class UserTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateUser()
    {
        var email = Email.Create("luffy@pirates.com").Value;
        var user = User.Create(email, "Monkey D.", "Luffy");

        user.ShouldNotBeNull();
        user.Email.ShouldBe(email);
    }

    [Fact]
    public void AssignRole_ShouldAddRoleToUser()
    {
        var user = CreateValidUser();
        var role = Role.Create("Captain").Value;

        user.AssignRole(role);

        // Assert role is assigned
    }
}
```

**T3 — Email value object tests:**

```csharp
public class EmailTests
{
    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("missing@")]
    [InlineData("@domain.com")]
    public void Create_WithInvalidEmail_ShouldReturnFailure(string invalidEmail)
    {
        var result = Email.Create(invalidEmail);
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithValidEmail_ShouldReturnSuccess()
    {
        var result = Email.Create("luffy@pirates.com");
        result.IsSuccess.ShouldBeTrue();
    }
}
```

---

## 📊 Architecture Compliance

### Clean Architecture Layer Dependencies

```
✅ Domain         → No dependencies (verified by ArchitectureTests)
✅ Application    → Domain only
✅ Infrastructure → Application + Domain
✅ Api            → Infrastructure only
```

### SOLID Principles Assessment

| Principle | Status | Notes |
|-----------|--------|-------|
| **S** — Single Responsibility | ✅ Good | Handlers, validators, and entities have clear responsibilities |
| **O** — Open/Closed | 🟡 Partial | Database provider selection uses switch statement (not extensible) |
| **L** — Liskov Substitution | ✅ Good | Proper interface usage throughout |
| **I** — Interface Segregation | ✅ Good | Small, focused interfaces (`ICommand`, `IQuery`, `IRepository`) |
| **D** — Dependency Inversion | ✅ Good | All cross-layer dependencies use interfaces |

### DDD Compliance

| Principle | Status | Notes |
|-----------|--------|-------|
| Aggregate roots | ✅ | Clear aggregate boundaries with factory methods |
| Encapsulation | 🔴 | Public setters violate encapsulation on domain entities |
| Value objects | ✅ | Email, Code, TypedId — immutable with validation |
| Domain events | 🟡 | Infrastructure exists but limited usage |
| Repositories | ✅ | Interfaces in domain, implementations in infrastructure |
| Specifications | ✅ | Clean implementation with composition |
| Ubiquitous language | ✅ | Business-oriented naming throughout |

---

## 🗺️ Prioritized Improvement Roadmap

### Phase 1 — Critical (Security & Correctness)

| # | Task | Priority | Effort |
|---|------|----------|--------|
| 1 | Add global exception handler in API layer | 🔴 High | Low |
| 2 | Fix tenant filter bypass when `CurrentTenantId` is null | 🔴 High | Low |
| 3 | Add null checks in `AssignRoleToUserCommandHandler` | 🔴 High | Low |
| 4 | Add environment check for development certificates | 🔴 High | Low |
| 5 | Handle JSON parsing exceptions in `CommandEndpointMapper` | 🔴 High | Low |

### Phase 2 — DDD & Encapsulation

| # | Task | Priority | Effort |
|---|------|----------|--------|
| 6 | Replace public setters with domain methods on User, Tenant, Role, Permission | 🟡 Medium | Medium |
| 7 | Add validation to User constructor (name, surname) | 🟡 Medium | Low |
| 8 | Fix Email implicit conversion to use safe pattern | 🟡 Medium | Low |
| 9 | Fix Code double-call in explicit conversion | 🟡 Medium | Low |
| 10 | Extract claim type constants to shared location | 🟡 Medium | Low |

### Phase 3 — Testing

| # | Task | Priority | Effort |
|---|------|----------|--------|
| 11 | Add User domain entity unit tests | 🟡 Medium | Medium |
| 12 | Add Tenant domain entity unit tests | 🟡 Medium | Low |
| 13 | Add Email and Code value object tests | 🟡 Medium | Low |
| 14 | Add PermissionCollection tests | 🟡 Medium | Low |
| 15 | Add middleware unit tests (ValidationMiddleware, PermissionAuthorizationMiddleware) | 🟡 Medium | Medium |
| 16 | Add null-return scenario tests for handlers | 🟡 Medium | Low |

### Phase 4 — Architecture & Design

| # | Task | Priority | Effort |
|---|------|----------|--------|
| 17 | Add tenant existence validation in TenantMiddleware | 🟢 Low | Medium |
| 18 | Protect tenant ID on entity updates in TenantInterceptor | 🟢 Low | Medium |
| 19 | Add soft delete to core entities (User, Role, Permission) | 🟢 Low | Low |
| 20 | Default endpoints to RequireAuthorization() | 🟢 Low | Low |
| 21 | Enable nullable reference types across all projects | 🟢 Low | High |
| 22 | Add safe type conversions in QueryEndpointMapper | 🟢 Low | Medium |

### Phase 5 — New Features & DX

| # | Task | Priority | Effort |
|---|------|----------|--------|
| 23 | Add structured logging middleware | 🟢 Low | Medium |
| 24 | Add health check endpoints | 🟢 Low | Low |
| 25 | Add request/response correlation IDs | 🟢 Low | Medium |
| 26 | Add pagination support for list queries | 🟢 Low | Medium |
| 27 | Add domain event publishing mechanism | 🟢 Low | High |
| 28 | Add API rate limiting middleware | 🟢 Low | Medium |
| 29 | Cache compiled specification expressions | 🟢 Low | Low |
| 30 | Add OpenAPI response schemas for all endpoints | 🟢 Low | Medium |

---

## 🆕 Suggested New Features

### 1. Global Exception Handling Middleware

Standardize error responses across all endpoints:

```csharp
public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { ... });
        }
        catch (UnauthorizedException)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { ... });
        }
    }
}
```

### 2. Domain Event Publishing

Extend aggregates with domain event support:

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### 3. Health Check Endpoints

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<RaftelDbContext>()
    .AddCheck("self", () => HealthCheckResult.Healthy());

app.MapHealthChecks("/health");
```

### 4. Structured Logging

Add correlation IDs and structured logging for observability:

```csharp
public sealed class CorrelationIdMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"]
            .FirstOrDefault() ?? Guid.NewGuid().ToString();

        context.Response.Headers["X-Correlation-Id"] = correlationId;
        using (logger.BeginScope(new { CorrelationId = correlationId }))
        {
            await next(context);
        }
    }
}
```

### 5. Pagination Support

Standardized pagination for list queries:

```csharp
public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IPagedQuery<TResult> : IQuery<PagedResult<TResult>>
{
    int Page { get; }
    int PageSize { get; }
}
```

---

## ✅ Best Practices Already Followed

| Practice | Status |
|----------|--------|
| Feature-folder organization | ✅ |
| Strongly typed IDs | ✅ |
| Result pattern for error handling | ✅ |
| Factory methods for aggregate creation | ✅ |
| Interface-based dependency injection | ✅ |
| Centralized package management (Directory.Packages.props) | ✅ |
| Architecture tests with NetArchTest | ✅ |
| Testcontainers for integration tests | ✅ |
| CQRS with command/query separation | ✅ |
| Middleware pipeline for cross-cutting concerns | ✅ |
| Specification pattern for domain queries | ✅ |

## 📖 Recommended Best Practices to Adopt

| Practice | Benefit |
|----------|---------|
| Enable nullable reference types | Compile-time null safety |
| Add XML documentation to all public APIs | Better IntelliSense and documentation |
| Use `sealed` keyword on all non-inheritable classes | Performance and design intent |
| Add `[ApiController]` behavior to minimal API | Model validation and error responses |
| Implement `IAsyncDisposable` on DbContext | Proper async resource cleanup |
| Add code coverage reporting to CI | Track coverage over time |
| Use `TimeProvider` abstraction for testable time | Avoid `DateTime.Now` in domain |

---

## 📈 Coverage Summary

| Layer | Estimated Coverage | Target |
|-------|-------------------|--------|
| Domain — Abstractions | 90% | 95% |
| Domain — Entities | 55% | 90% |
| Domain — Value Objects | 40% | 90% |
| Application — Handlers | 100% | 100% |
| Application — Validators | 100% | 100% |
| Application — Middlewares | 35% | 80% |
| Infrastructure — Repositories | 70% | 85% |
| Infrastructure — Interceptors | 50% | 75% |
| API — Endpoints | 60% | 80% |
| **Overall** | **~70-75%** | **85%** |

---

## 🔐 Security Findings

| # | Severity | Finding | Recommendation |
|---|----------|---------|----------------|
| S1 | 🔴 Critical | Development certificates used in all environments | Add environment check; use real certificates in production |
| S2 | 🔴 Critical | Null tenant ID bypasses tenant filter | Require tenant context for tenant-aware queries |
| S3 | 🟡 Medium | Auto-anonymous endpoints by default | Default to `RequireAuthorization()` |
| S4 | 🟡 Medium | Role assignment across tenants | Add tenant-aware user lookup in AuthenticationService |
| S5 | 🟡 Medium | No global exception handler | Stack traces exposed in production |
| S6 | 🟢 Low | Soft delete filter can be disabled without audit | Add logging when filters are disabled |

---

> This document serves as a guide for technical improvement, an evolutionary roadmap, and a quality baseline for the Raftel project. Each finding can be tracked as an individual issue or PR for incremental improvement.
