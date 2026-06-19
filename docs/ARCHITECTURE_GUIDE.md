# Architecture Guide

> How VPureLux is layered, how dependencies flow, and where each kind of logic belongs. Accurate to the current solution (`VPureLux.slnx`).

## Layers (projects)

| Project | Role |
| --- | --- |
| `VPureLux.Domain.Shared` | Enums, constants, error codes (`VPureLuxDomainErrorCodes`), localization resource (`VPureLuxResource`) and `Localization/VPureLux/*.json`. No behavior. |
| `VPureLux.Domain` | Aggregates/entities, value objects, domain services (`*Manager`), repository **interfaces**, domain events. Business invariants live here. |
| `VPureLux.Application.Contracts` | Application service **interfaces** (`I*AppService`), DTOs, `GetXListInput` query inputs, permission definitions (`VPureLuxPermissions`, `VPureLuxPermissionDefinitionProvider`). |
| `VPureLux.Application` | Application service implementations, per-module mapper classes (Mapperly), application-level validators. Orchestrates use cases. |
| `VPureLux.EntityFrameworkCore` | `VPureLuxDbContext`, EF Core entity configurations, repository **implementations** (`EfCore*Repository`), migrations. |
| `VPureLux.HttpApi` / `VPureLux.HttpApi.Client` | Auto API controllers over the application services, and the typed client/proxy. |
| `VPureLux.Web` / `VPureLux.Web.Public` | Razor Pages UI (PageModels, `.cshtml`, page JS), menus, theme. Hosts the app. |
| `VPureLux.DbMigrator` | Console host that migrates + seeds. |
| `test/*` | xUnit test projects (see below). |

## Dependency direction

References flow **inward toward the Domain** (verified from `.csproj` ProjectReferences):

```
Domain.Shared  ← Domain  ← Application  ← Web / Web.Public
      ↑            ↑           ↑              ↑
      └─ Application.Contracts ┘     EntityFrameworkCore ┘
                  ↑                        ↑
            HttpApi / HttpApi.Client       (Web references EF Core for hosting)
```

Concretely:
- `Domain` → `Domain.Shared`
- `Application.Contracts` → `Domain.Shared`
- `Application` → `Domain`, `Application.Contracts`
- `EntityFrameworkCore` → `Domain`
- `HttpApi`, `HttpApi.Client` → `Application.Contracts`
- `Web`, `Web.Public` → `Application`, `HttpApi`, `EntityFrameworkCore`

**Rule:** never add a reference that points outward (e.g. `Domain` must not reference `Application` or `EntityFrameworkCore`). Repository interfaces live in `Domain`; their EF Core implementations live in `EntityFrameworkCore`.

## Where business rules belong

- **Invariants and domain logic → `Domain`.** Entities protect their own state through methods (e.g. `Customer.Activate()`, `AssignGroup()`), and `*Manager` domain services enforce cross-entity rules and uniqueness (e.g. `CustomerManager.EnsureCodeIsUniqueAsync`, `EnsureGroupIsActiveAsync`). Violations throw `BusinessException(VPureLuxDomainErrorCodes.*)`.
- **Use-case orchestration → `Application`.** Application services check permissions, load aggregates via repositories, call domain managers/entity methods, persist, and map to DTOs. They do not contain raw invariant logic that belongs in the domain.
- **Persistence/query shaping → `EntityFrameworkCore`.** Custom repository implementations contain query/filter/paging logic and EF specifics.
- **Presentation only → `Web`.** PageModels translate HTTP requests into application-service calls and prepare view data.

## What must NOT be placed in the UI

- No direct `DbContext`, `IRepository`, or EF Core usage in PageModels or `.cshtml`.
- No domain services (`*Manager`) or entity construction in the UI.
- No business rules / calculations in Razor or JavaScript (e.g. pricing, stock math, status transitions). The UI calls application services and renders results.
- No persistence or transaction handling in the UI.

See `docs/UI_RAZOR_PAGES_GUIDE.md` for the full UI rule set and `docs/ABP_CODING_CONVENTIONS.md` for backend conventions.

## Cross-cutting concerns

- **Authorization:** permission constants in `VPureLuxPermissions`; enforced with `[Authorize(...)]` on application services (class + per method) and re-checked in the UI for visibility (`IAuthorizationService`).
- **Localization:** `VPureLuxResource` with `Localization/VPureLux/vi-VN.json` (primary culture `vi-VN`).
- **Errors:** coded `BusinessException`s defined in `VPureLuxDomainErrorCodes` (e.g. `CUSTOMER_001`), surfaced to users via localized messages.
- **Mapping:** Mapperly mapper classes per module in `Application` (see conventions).
- **Concurrency:** `RowVersion` concurrency tokens on selected inventory/sales aggregates (special-cased for SQLite in `VPureLuxDbContext.OnModelCreating`).

## Test layers

- `VPureLux.Domain.Tests` — pure domain/aggregate/manager unit tests.
- `VPureLux.Application.Tests` — application-level logic (e.g. catalog validation/image safety).
- `VPureLux.EntityFrameworkCore.Tests` — application service + repository integration tests over SQLite in-memory.
- `VPureLux.Web.Tests` — Razor Pages (`Pages/*`) and auto-API (`Api/*`) tests.
- `VPureLux.TestBase` — shared test infrastructure (in-memory data seeding, distributed-lock stub).
