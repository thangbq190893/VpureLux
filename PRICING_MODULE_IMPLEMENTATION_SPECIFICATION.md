# Pricing Module Implementation Specification

## Scope

Pricing owns only:

* Component Suggested Selling Price Versions
* Product Suggested Selling Price Versions

Pricing does not own Actual Selling Price, Inventory FIFO issue cost, Sales
profit calculation, customer-specific pricing, CustomerGroup pricing, or
actual component purchase/input cost.

## Aggregate Design

`ComponentSuggestedSellingPriceVersion` and `ProductSuggestedPriceVersion` are separate
aggregate roots. Existing versions are immutable except that the current active
version is closed when its successor is created. Price versions cannot be
updated or deleted.

Both aggregates contain:

* Item identifier
* Immutable `PriceVersionNo`
* Immutable VND `Money`
* Immutable `Reason`, maximum 500 characters
* `EffectiveFrom`, inclusive
* Nullable `EffectiveTo`, exclusive
* `PriceVersionStatus`

## Business Rules

* Currency is always VND.
* Price precision is `DECIMAL(18,2)`.
* Price must be greater than zero.
* Effective periods use `[EffectiveFrom, EffectiveTo)` semantics.
* Backdated version creation is rejected. A date before the current business
  date is backdated.
* Version numbers increment independently per Component or Product.
* Only one current active version exists per Component or Product.
* Creating a successor closes the current active version at the successor's
  `EffectiveFrom`.
* Catalog Components and Products must exist and be active before a price
  version is created.

## Domain Events

Events contain identifiers and lightweight metadata only:

* `ComponentSuggestedSellingPriceVersionCreatedEvent`
* `ComponentSuggestedSellingPriceVersionClosedEvent`
* `ProductSuggestedPriceVersionCreatedEvent`
* `ProductSuggestedPriceVersionClosedEvent`

## Application Contracts

Each price type exposes:

* Current price lookup
* Historical price lookup at a supplied date
* Version history
* New version creation

No update or delete contract is permitted.

## Permissions

* `Pricing.View`
* `Pricing.History`
* `Pricing.ComponentSuggestedSellingPrices.Create`
* `Pricing.ProductSuggestedPrices.Create`

## Persistence Requirements For STEP 05

* Prices map to `DECIMAL(18,2)`.
* Reason maps to `NVARCHAR(500)`.
* Currency is persisted as VND.
* Unique version number per owning item.
* Database-backed enforcement for one active version per owning item.

## Deferred Responsibilities

* Inventory owns FIFO lot issue cost and Sales `CostPriceSnapshot`.
* Sales owns Actual Selling Price, revenue, profit, and margin.
* Inventory Receipt `UnitCost` owns actual component purchase/input cost.
* Customer-specific and CustomerGroup pricing are future, separate pricing
  capabilities and are not part of phase 1.
