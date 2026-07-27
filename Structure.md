# ProjectBase — Architecture & Conventions

This is the generic DDD/CQRS starting point for every new backend project. It stays
project-agnostic on purpose (solution name, DB name, Swagger title etc. are meant to be
renamed per-project) — do not "fix" the generic naming, that's intentional.

Verified against `dotnet build` as of this writing: **0 errors, 0 warnings.** If you fork this
into a new project, run a clean build immediately and keep it that way — a template that
doesn't compile out of the box defeats the entire point of having one.

## Layers

```
Domain          → Entities, Value Objects, domain events, domain exceptions, repository
                  interfaces (write-side). Zero external dependencies except MediatR's
                  INotification (needed for IDomainEvent). Aggregates expose only factory
                  methods (Order.Create) and behavior methods, never public setters.

Application     → CQRS via MediatR. Features/<Aggregate>/Commands|Queries/<Name>/ each with
                  Command|Query.cs, ...Handler.cs, and (commands) a FluentValidation Validator.
                  Cross-cutting behavior lives in Application/Pipelines (IPipelineBehavior<,>):
                  validation, transaction-per-command, caching (ICachableQuery), perf logging.
                  Read-only access goes through Application/Abstractions/QueryRepositories
                  (I<X>QueryRepository → DTOs via AutoMapper ProjectTo), separate from the
                  write-side Domain.Repositories interfaces. Domain event handlers live under
                  Features/<Aggregate>/EventHandlers. External service contracts
                  (ICurrentUserService, INotificationService, IPhotoUploader) are declared in
                  Application/Abstractions/Services, implemented in Integration/WebApi.
                  Application/Common/ErrorCodes.cs holds the language-agnostic error code
                  constants — see "Error handling" below.

Infrastructure  → EF Core only. Persistence/Context (ApplicationDbContext, implements
                  IUnitOfWork, publishes domain events after SaveChangesAsync),
                  Persistence/Configurations (IEntityTypeConfiguration<T>),
                  Persistence/Repositories (write-side, BaseRepository<T> + aggregate-specific
                  subclasses), Persistence/QueryRepositories (read-side DTO projections),
                  Persistence/Interceptors (AuditableEntityInterceptor — sets CreatedAt/
                  CreatedBy/LastModified*/soft-delete automatically; registered in
                  Infrastructure/DependencyInjection/DependencyInjection.cs). Postgres/Npgsql
                  only — this was deliberately settled on, do not reintroduce a dual-provider
                  switch.

Integration     → Outward-facing adapters that aren't persistence: ICurrentUserService (reads
                  JWT claims via HttpContext), external API clients (IHttpClientFactory-based).

WebApi          → Composition root (Program.cs), thin controllers (delegate to ISender.Send),
                  SignalR hub, middleware, Hangfire dashboard, Swagger, Serilog/auth/CORS setup,
                  i18n resources.
```

**Rule to keep:** `DbContext` is only ever touched inside Infrastructure's Repository/
QueryRepository classes. MediatR Handlers never inject or call it directly. Controllers always
inherit from a base controller, always carry `[Authorize]` (or an explicit `[AllowAnonymous]`
if they're genuinely public), and read the current user/company from `ICurrentUserService`,
never from `[FromBody]`.

## Error handling contract

Every failure path — thrown exceptions and auth challenges alike — returns the same shape
(`WebApi/Contracts/ErrorResponse.cs`):

```json
{
  "success": false,
  "errorCode": "ORDER_NOT_FOUND",
  "message": "İstenen kayıt bulunamadı.",
  "traceId": "0HN7ABC123:00000001",
  "statusCode": 404,
  "errors": [ { "field": "zipCode", "errorCode": "VALIDATION_ERROR", "message": "..." } ]
}
```

- **Never throw a bare `Exception`.** Use `NotFoundException`, a `DomainException` subclass, or
  add a new one. Handlers/domain code only ever produce an `errorCode` — never a hardcoded,
  language-specific string.
- `errorCode` comes from `Application/Common/ErrorCodes.cs` (generic codes) or a
  project-specific extension of it (e.g. `OrderDomainException` can carry its own code like
  `"ORDER_ALREADY_SHIPPED"` via the `DomainException(message, errorCode)` constructor).
- `message` is resolved **once, centrally**, in `GlobalExceptionHandlingMiddleware` via
  `IStringLocalizer<SharedResource>` — never build it anywhere else.
- 500s never leak `exception.Message` to the client, in any environment. `debugDetail` (full
  `exception.ToString()`) is only ever populated when `IWebHostEnvironment.IsDevelopment()` —
  don't treat its absence as a security boundary on its own; it's a dev convenience.
- `traceId` is always populated (see Correlation ID below) — hand it to support instead of
  trying to describe the error over chat.

**Known gap, intentionally left for the specializing project:** `FieldError.message` for
validation failures still comes straight from FluentValidation's `ErrorMessage` (not yet run
through the localizer) — only the top-level `message`/`errorCode` are fully localized today.
Full per-field i18n means giving every validator's `.WithMessage(...)` a resource-key lookup,
which is a larger, per-validator change better done once a project has real validators to
migrate.

## Localization (i18n)

- Translations live in `WebApi/Resources/SharedResource.*.resx` — `SharedResource.resx` is
  Turkish (default), `SharedResource.en.resx` is English. Add a language by adding
  `SharedResource.<culture>.resx` and registering the culture in `Program.cs`
  (`supportedCultures`).
- To add a new error code: add the constant to `Application/Common/ErrorCodes.cs`, then add a
  matching `<data name="YOUR_CODE">` entry to **every** `.resx` file. If you forget, the
  middleware falls back to the generic `UNEXPECTED_ERROR` text rather than leaking the raw code
  — but that's a safety net, not a substitute for adding the translation.
- Culture resolution is automatic via `Accept-Language` (ASP.NET Core's default
  `RequestLocalizationOptions` providers) — clients don't need a custom header.

## Correlation ID & logging

- `WebApi/Middleware/CorrelationIdMiddleware.cs` resolves a per-request id (incoming
  `X-Correlation-Id` header, or the framework `TraceIdentifier` otherwise), pushes it into
  every Serilog log line via `LogContext`, echoes it on the response header, and it's what
  populates `traceId` in error responses. It must stay the outermost custom middleware in the
  pipeline so it's available even if something else fails.
- Logging is Serilog, configured from `appsettings*.json` (`Serilog:WriteTo`): Console + File
  always; Elasticsearch (via `Elastic.Serilog.Sinks`) for centralized search/Grafana. See
  `Observability/README.md` before deploying — in particular, **the Elasticsearch sink does
  not fail silently**: `Serilog.Debugging.SelfLog` (wired in `Program.cs`) writes sink
  connectivity errors to stderr, so a "no logs showing up" situation is diagnosable instead of
  a silent black hole.

## Authentication & authorization

- JWT Bearer, configured from `Auth:Authority` / `Auth:ApiName` in appsettings (these existed
  before but were dead config — now actually wired in `Program.cs`).
- **Secure by default**: `AddAuthorization` sets a `FallbackPolicy` requiring an authenticated
  user. A new controller/endpoint is closed unless explicitly marked `[AllowAnonymous]` — this
  is deliberate defense-in-depth so forgetting `[Authorize]` can't silently ship an open
  endpoint.
- 401/403 responses from the auth middleware itself (not just from application exceptions) use
  the exact same `ErrorResponse` shape (see `OnChallenge`/`OnForbidden` in `Program.cs`) — a
  client never sees two different error contracts depending on which layer rejected it.
- `NotificationHub` (SignalR) requires `[Authorize]` and joins each connection to a group named
  after the user's id (`uid` claim, matching `ICurrentUserService.UserId`) so
  `INotificationService.SendNotificationToUserAsync` actually reaches that user.
- `/hangfire` is locked down by `WebApi/Filters/HangfireAuthorizationFilter.cs` — currently
  "any authenticated user can view it." Add a role/claim check before this ever holds real
  background job data for a live project.

## Starting a new project from this template

1. Rename the solution/namespaces/DB names from "ProjectBase" to your project.
2. Fill in real values for `ConnectionStrings`, `Auth:Authority`/`Auth:ApiName`,
   `Serilog:WriteTo:Elasticsearch:Args:nodes`, `CorsSettings:AllowedOrigins`.
3. Import `Observability/grafana/projectbase-api-overview.json` into Grafana and point its
   Elasticsearch datasource at your cluster.
4. Extend `Application/Common/ErrorCodes.cs` and the `.resx` files with your domain's error
   codes as you add features — keep using the same centralized-translation pattern, don't
   reintroduce hardcoded per-handler strings (this is exactly the drift that happened in the
   last project built on this base).
