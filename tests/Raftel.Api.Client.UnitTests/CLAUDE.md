# Raftel.Api.Client.UnitTests

**Unit** tests of `Raftel.Api.Client`. Currently cover `QueryFilter` (query parameter construction/serialization for API calls). No network or server.

## Stack

- **xUnit 2.9.3** + **Shouldly**.

## Conventions

- Pure, fast tests: input → expected filter string/structure.
- Keep coverage symmetric with how the server (`Raftel.Api.Server/AutoEndpoints`) interprets those parameters: a change in one should break here.
- One test file per tested type; Arrange/Act/Assert.
