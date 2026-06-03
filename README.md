# ApiRefactor — Wave Management API

A refactored .NET 9 Web API for managing waves (store-picking batches), addressed against the Coles candidate requirements.

---

## What Changed and Why

The original codebase had several critical problems:

- **SQL injection** via string-concatenated queries
- **Resource leaks** — connections opened without `using` / `IDisposable`
- **N+1 query** — `Waves` constructor opened one connection per wave
- **Business logic in models** — no separation of concerns whatsoever
- **No error handling, logging, auth, or tests**

The refactor introduces a proper layered architecture (Domain → Application → Infrastructure → API), making each concern independently testable and replaceable.

---

## Data Access: Dapper

**Choice: Dapper over EF Core.**

Reasons:

1. **Domain control.** The `Wave` entity has private setters and factory methods (`Create`, `Reconstitute`) to enforce invariants. EF Core requires either public setters or complex owned-entity configuration to avoid fighting the ORM. Dapper maps to a private flat DTO inside the repository and we reconstruct the entity explicitly — no ORM conventions to work around.

2. **Simplicity.** The schema is a single table with three columns. EF Core's migration infrastructure and change-tracking overhead buys nothing here.

3. **Transparency.** Every query is visible and explicit in the repository. There are no lazy-loading surprises or N+1 traps hiding behind navigation properties.

4. **Performance.** The list endpoint uses a single multi-query (`QueryMultipleAsync`) to fetch the total count and the page in one round-trip. Dapper makes this natural; with EF Core you'd need to be careful to avoid two separate `.CountAsync()` / `.ToListAsync()` calls.

**What EF Core would buy:** automatic migrations, change tracking for complex graphs, LINQ queries. If the schema grows substantially, that tradeoff is worth revisiting.

---

## CQRS

**Applied: MediatR with separate command and query handlers.**

Wave operations decompose cleanly into two categories:

| Read | Write |
|---|---|
| `GetWavesQuery` | `UpsertWaveCommand` |
| `GetWaveByIdQuery` | |

The domain is small, so this isn't buying a separate read-model database. What it does buy:

- **Independent testability** — each handler is a plain class with no controller dependency
- **Pipeline behaviours** — `LoggingBehaviour` and `ValidationBehaviour` wrap every request uniformly without touching the handlers
- **Future-proofing** — if read and write throughput diverge significantly, read handlers can be pointed at a read replica without touching write logic

The counter-argument is that MediatR adds indirection for a simple CRUD API. That's a fair objection. For a domain this small, a direct service interface would also be defensible — but the requirement explicitly asked for CQRS consideration, and the pipeline behaviour pattern (`IPipelineBehavior<,>`) alone justifies the dependency for cross-cutting concerns.

---

## Minimal APIs vs Controllers

**Choice: Classic API Controllers.**

Reasons:

1. **Role-based auth is cleaner on controllers.** Applying `[Authorize(Roles = "...")]` at the action level is one attribute. With minimal APIs you'd either use `RequireAuthorization` inline on every `MapGet`/`MapPost` call or define policies — both are more boilerplate for fine-grained per-endpoint access control.

2. **Swagger/OpenDoc.** `[ProducesResponseType]` attributes on controllers give richer documentation with no extra setup. Minimal APIs require `WithOpenApi()` chaining on every endpoint.

3. **Grouping.** Controllers co-locate all wave endpoints in one file. With minimal APIs you'd need extension methods or route groups to avoid a cluttered `Program.cs`.

**Where minimal APIs would win:** a small number of endpoints with uniform auth, or a pure middleware-style handler where the overhead of a controller class is genuinely wasteful. This doesn't fit that profile.

---

## Authentication and Authorisation

**Scheme: JWT Bearer with role claims.**

Two roles are defined in `Roles.cs`:

| Role | Can do |
|---|---|
| `waves.reader` | `GET /api/waves`, `GET /api/waves/{id}` |
| `waves.writer` | All of the above + `POST /api/waves`, `PUT /api/waves/{id}` |

The `TokenController` (Development only) issues stub tokens at `/api/token/reader` and `/api/token/writer` so the auth layer can be exercised without an external identity provider.

**Production path:** replace `TokenController` and `TokenService` with Entra ID / Auth0 / Cognito integration. The `JwtSettings` section in `appsettings.json` already accepts `Issuer` and `Audience`, so swapping the signing key and validating against an OIDC discovery endpoint is a configuration change, not a code change.

---

## Design Patterns Applied

### Repository Pattern
`IWaveRepository` / `WaveRepository` — isolates all SQLite/Dapper code behind an interface. Controllers and handlers never touch a connection. This makes the storage technology swappable and the application layer fully unit-testable with `NSubstitute`.

### CQRS + MediatR Pipeline
Described above. The `ValidationBehaviour` and `LoggingBehaviour` pipeline behaviours are a practical application of the **Chain of Responsibility** pattern — cross-cutting logic applied uniformly without polluting handlers.

### Factory Methods on Domain Entity
`Wave.Create()` and `Wave.Reconstitute()` — prevent the entity from being instantiated in an invalid state. The parameterless constructor is private; Dapper maps through a private `WaveRow` DTO inside the repository, keeping the domain model honest.

### Options Pattern (Settings)
`JwtSettings` is bound via `IConfiguration` and injected as a typed singleton. No magic strings floating around JWT configuration.

### Patterns deliberately not applied

**Unit of Work:** Not applied. There is one aggregate (`Wave`) and no cross-aggregate transactions in scope. Adding `IUnitOfWork` would be purely ceremonial here. If a future requirement introduces, say, `WaveItems` that must be saved atomically with a `Wave`, this should be revisited.

**Result Pattern (e.g. `OneOf<T, Error>`):** Considered. Decided against it because the global exception middleware already provides a clean separation — handlers throw domain-meaningful exceptions (`KeyNotFoundException`, `ValidationException`) and the middleware translates them to HTTP. A Result type would add noise in handlers for no additional safety benefit at this scale.

---

## Performance Decisions

- **Pagination:** `GET /api/waves` requires `page` and `pageSize` query params (default 1/20, max 100). The list query never loads all rows.
- **Single round-trip for paged list:** `QueryMultipleAsync` executes `COUNT(*)` and the paginated `SELECT` in a single database call.
- **`CancellationToken` propagated throughout:** Every repository method and handler accepts and forwards the token, so client disconnects abort in-flight queries.
- **`IDbConnectionFactory` creates short-lived connections:** Connections are opened per-operation and disposed via `using`. SQLite handles its own file-level locking; long-held connections would block writers.
- **No unnecessary allocations:** Dapper's `QueryAsync<WaveRow>` maps directly to a `sealed record`; `ToList()` is called once at the repository boundary. The controller maps the domain list to DTOs in a single `Select`.

---

## What I Would Do Next

1. **HTTP integration tests** using `WebApplicationFactory<Program>` and an in-memory SQLite database — the `FixedConnectionFactory` pattern in the repository tests could be lifted to the full API level.
2. **Health check endpoint** (`/health`) reporting SQLite connectivity — a one-liner with `AddSqliteHealthCheck` and `MapHealthChecks`.
3. **OpenAPI / Swagger** with `Swashbuckle` or the new `Microsoft.AspNetCore.OpenApi` package.
4. **`ETag` / `If-None-Match` caching** on the list endpoint to avoid redundant data transfer for polling clients.
5. **Replace the stub token issuer** with a real OIDC provider integration.
6. **`WaveDate` timezone discipline** — currently stored and returned as ISO 8601 UTC strings; a UI that works across timezones will want the API to be explicit about this (the domain already enforces `DateTimeKind.Utc` via the validator).
7. **Soft-delete / archive** rather than hard-delete if waves represent immutable picking batches.
