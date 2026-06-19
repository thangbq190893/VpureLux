# VPureLux ERP — Project Overview

> Canonical, source-accurate overview for developers and AI agents. Keep this file in sync with the code. Where a statement is not verifiable from source, it is marked **Needs verification**.

## What VPureLux is

VPureLux is a **layered ERP web application** built on the **ABP Framework (commercial, v10.x)** using **Domain-Driven Design**. It manages a manufacturing/trading catalog of components and finished products, their bills of materials (BOM), suggested pricing, customer records, inventory movements, and sales orders, with a business audit trail. The UI is **ASP.NET Core Razor Pages** with the **LeptonX** theme, and the primary language is **Vietnamese (`vi-VN`)**.

The solution is a single deployable monolith with two web hosts:
- `VPureLux.Web` — the admin/operator application and OpenIddict auth server (`https://localhost:44325`).
- `VPureLux.Web.Public` — the public-facing site (`https://localhost:44304`), depends on the Web auth server.
- `VPureLux.DbMigrator` — console app that applies EF Core migrations and seeds initial data.

See `README.md` for setup/build and `AGENTS.md` for environment-specific (Cursor Cloud) caveats.

## Major business modules (present in source)

These are the **custom** VPureLux modules (under `src/VPureLux.Domain/<Module>`, with matching Application, EF Core, Web, and test code). See `docs/MODULE_MAP.md` for details.

| Module | Purpose (from code) |
| --- | --- |
| **Catalog** | Manage `Component` and `Product` master data (incl. catalog images). |
| **Bom** | Bill-of-materials versions per product, with draft → published → archived lifecycle. |
| **Customers** | `Customer` and `CustomerGroup` master data with active/inactive status. |
| **Pricing** | Component suggested selling prices and product suggested prices, kept as effective-dated versions. |
| **Inventory** | Warehouses, stock items, lots, transactions (receipt/issue/adjustment), and balances. |
| **Sales** | Sales orders with line items, BOM snapshot, confirm/cancel lifecycle, cost/profit views. |
| **Audit** | Business audit log (domain-level audit, distinct from ABP's framework audit logging). |
| **Dashboard** | Host and Tenant dashboards (permission-gated landing pages). |

Beyond these, the app depends on standard ABP commercial modules (Identity Pro, OpenIddict Pro, SaaS, CMS Kit Pro, File Management, Chat, Language Management, Text Template Management, GDPR, Setting/Feature/Permission/Audit Logging). These are configured in `VPureLuxDbContext` and the module classes; we do not own their code.

## Tech stack

- **.NET 10**, C# (`LangVersion: latest`).
- **ABP Framework 10.x** (commercial packages from the private ABP feed — see `AGENTS.md`).
- **EF Core** with SQL Server (production/dev) and **SQLite in-memory** for integration tests.
- **Razor Pages** + ABP Tag Helpers + **LeptonX** theme; client libraries via `abp install-libs`.
- **Mapperly** (`Riok.Mapperly` via `Volo.Abp.Mapperly`) for object mapping — **not AutoMapper**.
- **Redis** for distributed locking; **OpenIddict** for auth.
- Tests: **xUnit** + **Shouldly** + ABP test base.

## In scope (this documentation task)

- Create and maintain the `docs/` documentation set and a phased implementation roadmap.
- Keep documentation accurate to the current source tree.

## Out of scope (this documentation task)

- Implementing or changing business features, Domain/Application/EF/Web behavior.
- Creating EF Core migrations or changing the database schema.
- Modifying generated code (except to fix broken markdown links).

## Relationship to existing root docs

The repository root already contains many design/spec/audit documents (e.g. `MODULE_SPECIFICATIONS_V2.md`, `V2_ARCHITECTURE_ALIGNMENT_REPORT.md`, `UI_ABP_IMPLEMENTATION_RULES.md`, `*_MODULE_IMPLEMENTATION_SPECIFICATION.md`, `UI_*`/`CODEX_*` files). Those are historical/working specs and may not all match current code. The `docs/` set is the **concise canonical entry point**; it references those root docs where useful but the **source code is the source of truth**.
