# Walletly — notes for Claude Code

Walletly is an API-first Wallet-as-a-Service MVP for embedding NGN fiat wallets and payments. It is a .NET 10 ASP.NET Core modular monolith with DDD and a `.slnx` solution.

Working conventions for this repo. Match them so generated code looks hand-written.

## Scope (MVP)

- **Open-loop NGN fiat only.** No crypto and no multi-currency.
- **Deferred post-MVP:** escrow and tiered KYC. Do not build them unless asked.
- **Tenancy:** single database, multi-tenant by row (not schema-per-tenant).

## Architecture (dependencies point inward)

- **SharedKernel**: `Result` / `Result<T>`, `Error` / `ErrorType`, `ValidationError`, `Entity`, `IDomainEvent`, `IDomainEventHandler<T>`, `IDateTimeProvider`. No dependencies.
- **Walletly.Domain**: entities, ledger math, domain events, error catalogs. References SharedKernel only. Pure business logic: no EF, no ASP.NET, no application types.
- **Walletly.Application**: use cases as vertical slices, `Wallets/{UseCase}/`, `Payments/{UseCase}/`, and so on. `ICommand` / `IQuery` plus handlers, FluentValidation validators, validation and logging decorators (Scrutor), and abstractions such as `IPaymentGateway`, `IApplicationDbContext`, and auth. References Domain and SharedKernel.
- **Walletly.Infrastructure**: EF Core + PostgreSQL (snake_case), migrations, implementations of Application abstractions (payment gateways, email, etc.), Hangfire background jobs, domain-event dispatch. References Application.
- **Walletly.Api**: HTTP only. Controllers route and translate; they hold no business logic. Composition root: `Program.cs` wires Application interfaces to Infrastructure implementations.

Mental model for readers coming from Laravel: Application ≈ FormRequest + service/action classes, Domain ≈ pure business objects, Infrastructure ≈ Eloquent + third-party SDK wrappers, Api ≈ slim controllers.

## Conventions

- **CQRS, no MediatR.** `ICommand` / `ICommand<T>` with `ICommandHandler<...>`, and `IQuery<T>` with `IQueryHandler<Q, R>`. Handlers are `internal sealed` with primary constructors, registered by Scrutor scanning, and wrapped by `ValidationDecorator` and `LoggingDecorator`.
- **Errors, not exceptions.** Return `Result.Failure(...)` with an `Error` for expected failures (insufficient funds, duplicate key, unknown wallet). Error codes are `"{FeaturePlural}.{Reason}"`, for example `"Wallets.InsufficientFunds"`. Controllers translate with `result.Match(Results.Ok, ...)` or an equivalent problem mapper.
- **Controllers** are thin. Protect them with `[Authorize]`, and add permission checks where a permission model exists.
- **Money-moving endpoints** require an `Idempotency-Key` header, validated and stored so a retry returns the original result instead of charging twice.
- **Concurrency:** balance changes use pessimistic locking, `SELECT ... FOR UPDATE` via raw SQL, inside a transaction. This is a hard requirement to prevent double-spending. Do not replace it with optimistic checks alone.
- **Money:** store amounts as integer minor units (kobo) with an explicit currency code. Never use `float` or `double`.
- **Validation:** FluentValidation `AbstractValidator<TCommand>`, run in the validation decorator.
- **Data:** inject `IApplicationDbContext`. Entities reference each other by FK id (shadow relationships), with no navigations. Configurations in `Walletly.Infrastructure/{Feature}/` are auto-applied.
- **List queries** return `PagedList<T>` (search, sort, page).
- **Background jobs:** Hangfire, registered in Infrastructure.

## Security

- When adding a package or project reference, check for known-vulnerable transitive dependencies (`dotnet list package --include-transitive`, watch for NU1903 warnings). Pin them directly rather than ignoring the warning.
  - Precedents: `Microsoft.OpenApi` pinned to 2.7.5+ (GHSA-v5pm-xwqc-g5wc). `Newtonsoft.Json` pinned to 13.0.4 because Hangfire.Core pulled in 11.0.1 (GHSA-5crp-9r3c-p9vr).

## Build & test

- Build: `dotnet build Walletly.slnx`
- Test: `dotnet test Walletly.slnx`. Integration tests use Testcontainers, so Docker must be running. There is no test project yet.
- Keep code warning-clean. Prefer collection expressions (`[.. x]`) and `ToUpperInvariant`.
- Once adopted: `TreatWarningsAsErrors` with SonarAnalyzer.
