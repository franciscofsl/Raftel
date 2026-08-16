## 1. `ErrorType` and typed `Error`

- [x] 1.1 Write `ErrorTests`: two-arg construction defaults to `ErrorType.Failure`; each typed factory (`Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Failure`) produces the matching `ErrorType`; `Error.None` is equal by value to a two-arg `Error` with empty code/message
- [x] 1.2 Add `ErrorType` enum in `src/Raftel.Domain/Abstractions/ErrorType.cs` (`Failure`, `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Unexpected`)
- [x] 1.3 Extend `src/Raftel.Domain/Abstractions/Error.cs`: add `Type` with default `ErrorType.Failure`, add typed factory methods, change `None`/`NullValue` to `static readonly`
- [x] 1.4 Build the full solution and confirm no existing call site breaks (default parameter keeps compatibility)

## 2. HTTP translation (`ErrorResults`)

- [x] 2.1 Write `ErrorMappingTests` (functional): a handler returning `Error.NotFound(...)` responds 404 with a Problem Details body containing the `code` extension; repeat for `Conflict`→409, `Validation`→400, `Forbidden`→403, `Unauthorized`→401, `Failure`→400
- [x] 2.2 Implement `src/Raftel.Api.Server/AutoEndpoints/ErrorResults.cs` with `ToProblem(Error)`, `StatusCodeFor(ErrorType)`, `TitleFor(ErrorType)`
- [x] 2.3 Wire `ErrorResults.ToProblem` into `CommandEndpointMapper` (both `MapCommandEndpoint` and `MapCommandEndpointWithResult`), replacing `Results.BadRequest(result.Error)`
- [x] 2.4 Wire `ErrorResults.ToProblem` into `QueryEndpointMapper`, replacing `Results.BadRequest(result.Error)`
- [x] 2.5 Run the new functional tests and confirm all status codes and body shapes match (`dotnet test tests/Raftel.Api.FunctionalTests` with Docker available: 22/22 passed)

## 3. Success status codes

- [x] 3.1 Write functional tests: command without a result value → 204 empty body; command with a result value and `CreatedRouteName` set → 201 + `Location` header; command with a result value and no `CreatedRouteName` → 200 + body
- [x] 3.2 Add `CreatedRouteName` (nullable) to `src/Raftel.Api.Server/AutoEndpoints/CommandDefinition.cs`
- [x] 3.3 Update `CommandEndpointMapper.MapCommandEndpoint` to return 204 on success
- [x] 3.4 Update `CommandEndpointMapper.MapCommandEndpointWithResult` to return 201+Location when `CreatedRouteName` is set, else 200+body
- [x] 3.5 Confirm query endpoints are untouched and still return 200 on success (QueryEndpointMapper success path unchanged — only the error branch was rewired to ErrorResults)

## 4. Migrate framework and demo errors

- [x] 4.1 Categorize `src/Raftel.Domain/Features/Users/UserErrors.cs` with typed factories
- [x] 4.2 Categorize `src/Raftel.Domain/Features/Tenants/TenantErrors.cs` with typed factories
- [x] 4.3 Categorize `src/Raftel.Domain/Features/Authorization/RoleErrors.cs` with typed factories
- [x] 4.4 Categorize all `demo/Raftel.Demo.Domain/**/*Errors.cs` with typed factories
- [x] 4.5 Grep the framework and demo source trees for any remaining `new Error(...)` or `Error.Failure(...)` call classifiable under a more specific type, and reclassify (also caught `CreatePirateErrors.cs`, `GetTenantQueryHandler`, `GetPirateByIdQueryHandler`, `Code.cs`, `AuthenticationService.cs`; the four remaining `AuthenticationService` Identity-provider failures are explicitly `ErrorType.Unexpected`, not left at the `Failure` default by omission)

## 5. Regression and documentation

- [x] 5.1 Run the full test suite; update every assertion in `tests/Raftel.Api.FunctionalTests` that expected the old `400`-always / `200`-always behavior to the correct status code (found and fixed `AuthenticationTest.Register_NewUser_ShouldReturnOk` → `..._ShouldReturnNoContent`, asserting `204`; audited every other status-code assertion and body-read in the project, none else were affected)
- [x] 5.2 Confirm `dotnet test` passes across the whole solution — all 8 test projects pass with Docker available: `Raftel.Domain.Tests` 76/76, `Raftel.Application.UnitTests` 80/80, `Raftel.Shared.Tests` 14/14, `Raftel.Api.Client.UnitTests` 8/8, `Raftel.ArchitectureTests` 7/7, `Raftel.Api.FunctionalTests` 22/22, `Raftel.Infrastructure.Tests` 143/143, `Raftel.Application.IntegrationTests` 6/6; `dotnet build Raftel.sln` succeeds with 0 errors
- [x] 5.3 Document the three breaking changes (immutable `Error.None`/`NullValue`, new success codes, `ProblemDetails` body shape) in `BREAKING_CHANGES.md`
