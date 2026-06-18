# Customer Module Implementation Specification

Status: Approved implementation baseline

## 1. Architecture Boundary

Customer is an independent module implemented before Sales.

Customer owns current customer master data and configurable customer groups.
Customer does not own sales history, transaction prices, revenue, profit,
customer-specific price lists, credit limits, payment terms, contracts, accounts
receivable, inventory, or StockItems.

Sales will reference Customer by stable identifier and will own historical
customer, pricing, cost, revenue, and profit snapshots.

## 2. Scope

Phase 1 includes:

- Customer master-data management.
- Configurable Customer Groups.
- Customer and Customer Group activation and deactivation.
- Search, filtering, permissions, HTTP APIs, Razor Pages, persistence, data
  seeding, and automated tests.

Phase 1 excludes:

- Customer-specific pricing.
- Customer sales-history views and reports.
- Credit and accounting features.
- Inventory and Pricing integration.

## 3. Customer Aggregate

Customer is an Aggregate Root with:

- Id
- Code
- Name
- CustomerGroupId
- Status
- PhoneNumber
- Email
- Address
- TaxCode
- Notes

Behaviors:

- UpdateInfo
- AssignGroup
- Activate
- Deactivate

Invariants:

- Code is required, normalized, unique among non-deleted Customers, and
  immutable after creation.
- Name is required.
- CustomerGroupId is required.
- Only an active Customer Group can be assigned.
- Inactive Customers cannot be used for new Sales Orders.
- Historical references remain valid after Customer deactivation or soft
  deletion.

Domain events:

- CustomerCreatedEvent
- CustomerUpdatedEvent
- CustomerGroupChangedEvent
- CustomerActivatedEvent
- CustomerDeactivatedEvent

## 4. CustomerGroup Aggregate

CustomerGroup is configurable aggregate data. It must not be implemented as an
enum.

CustomerGroup properties:

- Id
- Code
- Name
- Description
- Status
- SortOrder

Behaviors:

- UpdateInfo
- Activate
- Deactivate

Invariants:

- Code is required, normalized, unique among non-deleted Customer Groups, and
  immutable after creation.
- Name is required.
- Inactive Customer Groups cannot be assigned.
- A Customer Group referenced by Customers cannot be hard deleted.

Domain events:

- CustomerGroupCreatedEvent
- CustomerGroupUpdatedEvent
- CustomerGroupActivatedEvent
- CustomerGroupDeactivatedEvent

Default seeded groups:

- RETAIL / Retail
- DEALER / Dealer
- DISTRIBUTOR / Distributor
- PROJECT / Project

Default group seeding uses stable identifiers, is idempotent, and must not
overwrite customized names.

## 5. Domain Services and Repositories

CustomerManager:

- Enforces unique Customer codes.
- Validates Customer Group existence and active status.
- Creates Customer aggregates.

CustomerGroupManager:

- Enforces unique Customer Group codes.
- Creates Customer Group aggregates.

Repository interfaces:

- ICustomerRepository
- ICustomerGroupRepository

Application services use repositories only and must not access DbContext
directly.

## 6. Validation

Customer:

- Code: required, normalized, maximum 50 characters.
- Name: required, maximum 200 characters.
- CustomerGroupId: required.
- PhoneNumber: optional, maximum 30 characters.
- Email: optional, valid email, maximum 256 characters.
- Address: optional, maximum 500 characters.
- TaxCode: optional, maximum 50 characters.
- Notes: optional, maximum 1000 characters.

Customer Group:

- Code: required, normalized, maximum 50 characters.
- Name: required, maximum 200 characters.
- Description: optional, maximum 500 characters.
- SortOrder: non-negative.

## 7. Error Codes

- CUSTOMER_001: Customer Code Already Exists
- CUSTOMER_002: Customer Not Found
- CUSTOMER_003: Customer Inactive
- CUSTOMER_004: Customer Group Not Found
- CUSTOMER_005: Customer Group Inactive
- CUSTOMER_006: Customer Group Code Already Exists
- CUSTOMER_007: Customer Group Is In Use

Business rule violations use BusinessException with documented error codes.

## 8. Permissions

- Customers.View
- Customers.Create
- Customers.Edit
- Customers.ManageStatus
- CustomerGroups.View
- CustomerGroups.Create
- CustomerGroups.Edit
- CustomerGroups.ManageStatus

## 9. Database Design

AppCustomerGroups:

- Id UNIQUEIDENTIFIER PK
- Code NVARCHAR(50) NOT NULL
- Name NVARCHAR(200) NOT NULL
- Description NVARCHAR(500) NULL
- Status TINYINT NOT NULL
- SortOrder INT NOT NULL
- ABP audit, concurrency, and soft-delete columns

Indexes:

- Unique filtered active Code index.
- Status and SortOrder index.

AppCustomers:

- Id UNIQUEIDENTIFIER PK
- Code NVARCHAR(50) NOT NULL
- Name NVARCHAR(200) NOT NULL
- CustomerGroupId UNIQUEIDENTIFIER NOT NULL
- Status TINYINT NOT NULL
- PhoneNumber NVARCHAR(30) NULL
- Email NVARCHAR(256) NULL
- Address NVARCHAR(500) NULL
- TaxCode NVARCHAR(50) NULL
- Notes NVARCHAR(1000) NULL
- ABP audit, concurrency, and soft-delete columns

Constraints and indexes:

- CustomerGroupId FK with Restrict delete behavior.
- Unique filtered active Code index.
- Name index.
- CustomerGroupId and Status index.

## 10. HTTP API

Customers:

- GET /api/customers
- GET /api/customers/{id}
- POST /api/customers
- PUT /api/customers/{id}
- POST /api/customers/{id}/activate
- POST /api/customers/{id}/deactivate

Customer Groups:

- GET /api/customer-groups
- GET /api/customer-groups/{id}
- POST /api/customer-groups
- PUT /api/customer-groups/{id}
- POST /api/customer-groups/{id}/activate
- POST /api/customer-groups/{id}/deactivate

Controllers delegate exclusively to application services.

## 11. Razor Pages

Customer pages:

- /Customers/Index
- /Customers/Create
- /Customers/Edit/{id}
- /Customers/Details/{id}

Customer Group pages:

- /CustomerGroups/Index
- /CustomerGroups/Create
- /CustomerGroups/Edit/{id}
- /CustomerGroups/Details/{id}

All actions and navigation are permission-aware and localized.

## 12. Sales Dependency

Future Sales Order creation requires CustomerId and validates that the Customer
is active.

Sales snapshots:

- CustomerId
- CustomerCodeSnapshot
- CustomerNameSnapshot
- CustomerGroupIdSnapshot
- CustomerGroupCodeSnapshot
- CustomerGroupNameSnapshot

Customer sales history belongs to Sales reporting and must not be added to the
Customer aggregate or Customer application services.

## 13. Frozen Cross-Module Decisions

- Inventory uses Generic StockItem architecture.
- Only Component StockItems are inventory-enabled in phase 1.
- Component creation will create a corresponding StockItem when Inventory is
  implemented.
- Component soft deletion will deactivate its StockItem while historical
  inventory remains intact.
- Product StockItems are architecturally supported but disabled in phase 1.
- Catalog Product and Component image support is approved but deferred and is
  not a Customer implementation blocker.
- Customer has no direct dependency on Catalog images, Pricing, Inventory, BOM,
  or Sales.

## 14. Required Tests

- Domain invariant and event tests.
- Application service, validation, permission, and error-code tests.
- Repository, EF mapping, foreign-key, unique-index, soft-delete, query, and
  migration tests.
- HTTP API tests.
- Razor Page authorization, localization, and validation tests.
- Default Customer Group idempotent seeding tests.

## 15. Implementation Sequence

1. Documentation Alignment
2. Domain Layer
3. Application Contracts
4. Application Layer
5. Infrastructure Layer
6. Web and HTTP API
7. Testing
8. Certification
