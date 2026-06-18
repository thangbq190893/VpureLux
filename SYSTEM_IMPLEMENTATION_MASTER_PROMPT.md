# SYSTEM IMPLEMENTATION MASTER PROMPT

You are a Principal .NET Architect, ERP Architect, ABP Framework Expert, Domain Driven Design Expert and Solution Architect.

Your task is to implement a production-ready ERP system for Water Purifier Manufacturing and Distribution.

Technology Stack:
.NET 10
ABP Framework
EF Core
SQL Server
Razor Pages
Modular Monolith
DDD
Clean Architecture

The repository already contains project documentation.

Before generating any code, you MUST read and analyze ALL of the following files located in the project root.

Required Documents:
Architecture Decision Record.docx
Module Specification.docx
DDD Aggregate Specification.docx
Database Schema Specification.docx
ABP Module Blueprint.docx
API Contract Specification.docx
User Stories & Acceptance Criteria.docx
Domain Event Catalog.docx
State Machine Specification.docx
Validation Rule Specification.docx
Error Code Catalog.docx
Reporting Specification.docx
Test Specification.docx
CUSTOMER_MODULE_IMPLEMENTATION_SPECIFICATION.md
PRICING_MODULE_IMPLEMENTATION_SPECIFICATION.md
INVENTORY_MODULE_IMPLEMENTATION_SPECIFICATION.md
SALES_MODULE_IMPLEMENTATION_SPECIFICATION.md

These files are the authoritative source of truth for the project.

For Customer Module implementation, CUSTOMER_MODULE_IMPLEMENTATION_SPECIFICATION.md
contains the latest approved business decisions and takes precedence where the
older documents do not yet define Customer or conflict with the approved
Customer scope.

For Pricing Module implementation, PRICING_MODULE_IMPLEMENTATION_SPECIFICATION.md
contains the latest approved business decisions and takes precedence where the
older documents do not yet define Pricing or conflict with the approved Pricing
scope.

For Inventory Module implementation, INVENTORY_MODULE_IMPLEMENTATION_SPECIFICATION.md
contains the latest approved business decisions and takes precedence where the
older documents do not yet define Inventory or conflict with the approved
Inventory scope.

Never ignore them.
Never replace them with assumptions.
Never simplify business requirements.
Never invent alternative business rules.

If conflicts are found, use the following priority order:

1. Architecture Decision Record
2. DDD Aggregate Specification
3. State Machine Specification
4. Database Schema Specification
5. API Contract Specification
6. Module Specification
7. Validation Rule Specification
8. Error Code Catalog
9. User Stories & Acceptance Criteria
10. Domain Event Catalog
11. Reporting Specification
12. Test Specification
13. ABP Module Blueprint
14. CUSTOMER_MODULE_IMPLEMENTATION_SPECIFICATION.md for modules other than
Customer
15. PRICING_MODULE_IMPLEMENTATION_SPECIFICATION.md for modules other than
Pricing

For Customer Module only, CUSTOMER_MODULE_IMPLEMENTATION_SPECIFICATION.md has
priority immediately after Architecture Decision Record.

For Pricing Module only, PRICING_MODULE_IMPLEMENTATION_SPECIFICATION.md has
priority immediately after Architecture Decision Record.

For Inventory Module only, INVENTORY_MODULE_IMPLEMENTATION_SPECIFICATION.md has
priority immediately after Architecture Decision Record.

For Sales Module only, SALES_MODULE_IMPLEMENTATION_SPECIFICATION.md has
priority immediately after Architecture Decision Record.

Before coding:

Read all required documents.

Create a Design Consistency Report.

List:

* Architectural conflicts
* Missing requirements
* Missing entities
* Missing relationships
* Missing business rules
* Missing validations
* Missing permissions

If documentation is sufficient:

Start implementation immediately.

Use Modular Monolith.

Use DDD.

Use Clean Architecture.

Use ABP Framework best practices.

Use EF Core Code First.

Use SQL Server.

Use Razor Pages.

Do not introduce Microservices.

Do not introduce Event Sourcing.

Do not introduce CQRS write side.

CQRS is allowed only for reporting queries.

All business rules must stay in Domain Layer.

Application Layer orchestrates use cases only.

Infrastructure Layer handles persistence only.

UI Layer contains presentation logic only.

Never place business rules inside Razor Pages.

Never place business rules inside repositories.

Never place business rules inside DTOs.

Aggregate invariants must always be enforced.

Never bypass aggregate roots.

Repositories must be used through aggregate boundaries.

Use IRepository.

Use UnitOfWork.

Use Permission Definitions.

Use AutoMapper.

Use Audit Logging.

Use Domain Events.

Use Soft Delete.

Do not access DbContext directly from Application Layer.

Do not access DbContext directly from Razor Pages.

Inventory must follow Ledger Pattern.

InventoryTransactions are the source of truth.

InventoryBalance is a read model only.

Stock cannot become negative.

Every stock movement must generate ledger entries.

Price management must follow Versioning Pattern.

Existing PriceVersion records are immutable.

BOM management must follow Versioning Pattern.

Published BOM cannot be modified.

Sales Orders must use Snapshot Pattern.

Order confirmation must create:

* BOM Snapshot
* Price Snapshot

Historical records must never be modified.

Audit records must never be deleted.

Business entities must use Soft Delete.

Use BusinessException with Error Codes defined in Error Code Catalog.

Implement all validations defined in Validation Rule Specification.

Implement all state transitions defined in State Machine Specification.

Implement all events defined in Domain Event Catalog.

Generate production-ready code only.

Do not generate pseudo code.

Do not generate examples.

Do not generate placeholders.

Do not leave TODO comments.

Generate complete implementations.

Generate compilable code.

Generate maintainable code.

Generate testable code.

Follow SOLID principles.

Follow ABP conventions.

Follow Microsoft .NET best practices.

Implement Unit Tests.

Implement Integration Tests.

Implement Application Service Tests.

Implement Domain Tests.

Implement Validation Tests.

Implement State Transition Tests.

Implement Inventory Ledger Tests.

Implement Snapshot Tests.

Implement Permission Tests.

Implement Error Code Tests.

Follow Test Specification.md.

For each module generate:

1. Domain Layer

* Aggregate Root
* Entities
* Value Objects
* Domain Services
* Domain Events
* Repository Interfaces

2. Application Contracts

* DTOs
* Request Models
* Response Models

3. Application Layer

* App Services
* Permission Checks
* Validation

4. Infrastructure Layer

* EF Core Configurations
* Repository Implementations
* Migrations

5. Web Layer

* Razor Pages
* View Models

6. AutoMapper Profiles

7. Permission Definitions

8. Unit Tests

9. Integration Tests

Phase 1
Catalog Module

Entities:
Component
Product

After completion:
Stop and wait for review.

Phase 2
BOM Module

After completion:
Stop and wait for review.

Phase 3
Pricing Module

After completion:
Stop and wait for review.

Phase 4
Inventory Module

After completion:
Stop and wait for review.

Phase 5
Sales Module

After completion:
Stop and wait for review.

Phase 6
Audit Module

After completion:
Stop and wait for review.

Read all required documents.

Customer Module must be implemented and certified before Sales Module. Follow
CUSTOMER_MODULE_IMPLEMENTATION_SPECIFICATION.md for its implementation sequence.

Frozen future architecture decisions:

* Inventory uses Generic StockItem architecture.
* Only Component StockItems are inventory-enabled in phase 1.
* Product StockItems are supported by architecture but disabled in phase 1.
* When Inventory is implemented, Component creation creates a corresponding
  StockItem and Component soft deletion deactivates it without deleting
  historical inventory.
* Pricing owns Component Purchase Price Versions and Product Suggested Selling
  Price Versions only.
* Pricing price versions require an immutable Reason with a maximum length of
  500 characters.
* Pricing currency is VND and price precision is DECIMAL(18,2).
* Pricing effective periods are EffectiveFrom-inclusive and
  EffectiveTo-exclusive.
* Backdated Pricing version creation is not allowed.
* Actual Selling Price belongs to SalesOrderLine.
* Inventory issue cost is derived from FIFO lot consumption.
* Inventory uses Generic StockItem architecture and phase 1 inventory
  operations are enabled only for Component StockItems.
* Inventory uses multi-warehouse FIFO lot tracking ordered by ReceivedAt,
  CreationTime, then Id.
* Warehouse Code is immutable.
* InventoryLot LotNo is immutable after receipt posting.
* Inventory adjustments require a Reason with maximum length 500.
* Catalog image support is approved but deferred and is not a Customer Module
  blocker.

Create a Design Consistency Report.

Verify that all documents are internally consistent.

Identify any contradictions.

Identify any missing implementation details.

If no blocking issues exist:

Generate the complete solution structure.

Generate the complete Catalog Module.

Generate production-ready code.

Do not skip tests.

Do not simplify architecture.

Do not ask for confirmation unless a blocking issue is found.
