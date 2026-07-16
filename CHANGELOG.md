# Changelog

All notable changes to Mohandseto Tawredat are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/) and Semantic Versioning per milestone (v0.1.0 → v1.0.0).

## [Unreleased] — v1.0.0 release candidate hardening

### Added
- System-wide typography accessibility pass: IBM Plex Sans Arabic is self-hosted across Admin/CRM and bundled in Flutter, with a uniform 1.4× text scale covering theme and hard-coded screen sizes while preserving device accessibility scaling.
- Final 756/756 screen closure: native Google Sign-In, Microsoft authorization-code/PKCE, server-side OIDC token validation, one-use external challenges, persistent provider-subject links, linked-account settings and 2FA continuation.
- Corrected design screen 48 to the actual invoice-export experience with date/status filters and server-generated PDF, XLSX and UTF-8 CSV downloads.
- `AddExternalAuthentication` migration, OAuth production setup guide, 93-test backend gate and clean 23-test Flutter gate.
- Release hardening for Milestone 10: production fail-fast configuration validation, persistent Data Protection keys, security headers, forwarded-header handling, response compression, API/body/rate limits and separate live/ready health probes.
- Multi-stage non-root API and Next.js containers, local and production Compose definitions, loopback-only production bindings, reverse-proxy example, persistent SQLite/key volumes and environment templates.
- Automated empty-database migration and authenticated platform E2E tests, PowerShell release smoke gate, CI container builds, dependency vulnerability audits and secret scanning.
- Product, architecture, database, OpenAPI, security, QA, staging/production/rollback and v1.0.0 release-checklist documentation.
- System monitoring and security for screens 743–756: service/database/storage/queue telemetry, sanitized error capture, security events and enforceable IP blocks, verified restore requests, version history and feature flags.
- Integrations Hub for screens 735–742: 11 provider cards, encrypted connection configuration, detailed WhatsApp/payment/ETA e-invoice/ERP workspaces, real database-backed operational metrics, manual run and temporary disable controls.
- Full integration operation lifecycle with server-side search/status/provider/date filters, CSV export, failure detail, error codes and endpoints, maximum-attempt guards, individual/bulk retry and a background due-retry worker.
- `AddIntegrationHub` migration, 81-test backend gate, clean Next.js lint/production build and fresh-database authenticated configure-test-run-log HTTP verification.
- System settings for screens 700–734: 29 schema-driven configuration sections plus delivery-zone, bank-account, API-key, Webhook, translation, integration-log and backup workspaces in a unified responsive RTL admin experience.
- `AddSystemSettings` migration, Data Protection encryption for provider credentials, one-time API/Webhook secret reveal with hash-only persistence, configurable login lockout/admin 2FA/minimum-password enforcement and immediate mobile maintenance/version/link gates.
- Real SQLite online backups with SHA-256 integrity, scheduled execution and safe retention cleanup; 76-test backend gate, clean Next.js lint/production build and authenticated live HTTP verification.
- Reporting center for screens 671–699: 22 database-backed business reports, KPI/trend/breakdown views, date/company/warehouse/staff/value filters, an eight-source custom report builder, reusable favorite templates, recurring delivery schedules and run history.
- Real XLSX and PDF generation and browser downloads, `AddReportingEngine` migration, background scheduled-report worker, full report audit trail, 71-test backend gate and clean Next.js lint/production build.
- Admin identity and access management for screens 656–670: platform users, role creation, permission matrix/module/action views, branch and warehouse scopes, real login-attempt logs, active sessions, suspension/reactivation, administrator password resets and immutable audit details.
- `AddAdminSystemAccess` migration, immediate JWT rejection for suspended accounts, 66-test backend gate, clean Next.js lint/build and fresh-database authenticated HTTP verification.
- Accounting operations for screens 600–619: financial KPIs, customer and tax invoices, bank-transfer recording and matching, outstanding balances and aging, statements, credit/debit notes, refunds, expenses, order/product/company profits, sales tax, XLSX export and guarded financial-period close.
- Returns and customer-service operations for screens 620–639: cross-company return review, secure photos, decisions, pickup and inspection, restock/dispose/replacement/credit-note dispositions, ticket chat and assignment, SLA policies, escalation, reply templates, staff ratings and issue-type reporting.
- `AccountingCustomerServiceOperations` migration, seeded SLA/templates, 60-test backend gate, clean Next.js production build/lint and authenticated dashboard/API HTTP verification.
- Shipping and delivery operations for screens 581–599: live KPIs and map, ready-order shipment creation, exact splitting, role-validated courier assignment, route planning and optimization, shipment timeline, delivery start and customer contact, mandatory photo/signature proof, receipt confirmation, partial delivery, failed attempts, rescheduling, courier performance and editable zone pricing.
- `ShippingDeliveryOperations` migration, secure GPS-aware proof storage, 58-test backend gate, clean Next.js shipping production build and fresh-database authenticated HTTP verification.
- Printing and design operations for screens 557–580: operational KPIs, design queue, customer brief, secure logo files and print-readiness review, workload-aware designer assignment, immutable design versions, customer dispatch and feedback, approval log, production samples, ordered production stages and quantities, quality gates, packaging, ready-to-ship notifications, late orders, design archive, company logo library and printed-product templates.
- `PrintingDesignOperations` migration, 56-test backend gate, clean Next.js printing production build, fresh-database verification and authenticated BFF dashboard/template verification.
- Contract lifecycle operations for screens 540–556: contract KPIs and filters, four-step creation, company eligibility, periods and renewals, included products, fixed or market-discount pricing, quantity tiers, payment and delivery terms, contract credit, attachments, sequential approvals, activation, renewal, scheduled price revisions, expiry alerts and contract-versus-market health.
- `ContractLifecycleOperations` migration, automatic effective-date price revision worker, 54-test backend gate, clean Next.js contracts production build and authenticated BFF activation verification.
- Company CRM operations for screens 509–539: company profiles, branches, users, document verification, classification, sales assignment, customer stages, calls, meetings, notes, tasks, commercial history, purchase analytics, upsell opportunities, contracts, special prices, credit, statements, support and account suspension/reactivation.
- `CompanyCrmOperations` migration, 52-test backend gate, clean Next.js CRM production build and authenticated BFF lifecycle verification.
- Supplier and procurement operations for screens 490–508: supplier profiles and price lists, comparison, ratings, documents, payables, purchase orders, partial/full receipts, returns, invoices, three-way matching and performance reporting.
- `SupplierProcurementOperations` migration, inventory-linked procurement lifecycle, 50-test backend gate and authenticated BFF procurement verification.
- Inventory and warehouse operations for screens 466–489: warehouse balances, immutable movement ledger, adjustments, transfers, reservations, counts and reconciliation, batches, serials, expiry, shelves, barcodes, receiving inspection, damaged rejection and valuation.
- `InventoryWarehouseOperations` migration, deterministic demo inventory, 49-test backend gate and authenticated BFF inventory HTTP verification.
- Content and home-page operations for screens 451–465: category and home-section reordering, banner scheduling and company targeting, pages, policies, FAQ, app notifications and in-app messages.
- `AdminContentOperations` migration, targeted delivery audit, 47-test backend gate and authenticated Next.js BFF HTTP verification.
- Commercial product operations for screens 426–450: packaging, cost and margin, company pricing, alternatives and related products, warranty, SEO, XLSX import error review, bulk price editing and auditable price history.
- `ProductCommercialOperations` migration and full service/XLSX automated coverage.
- Full admin quote-operations workspace for screens 404–425: RFQ queues, extraction review, product linking, supplier price requests and comparison, profit margins, versioned customer quotes, discounts, terms, negotiation, acceptance, order conversion and templates.
- `AdminQuoteOperations` migration with suppliers, temporary RFQ products, quote templates and commercial version fields.
- End-to-end admin quote lifecycle coverage plus authenticated HTTP verification through the Next.js BFF.
- Catalog API with categories, brands, products, search, filters, sorting, pagination, details, variants, quantity tiers, contract pricing, favorites, compare, and recently viewed.
- Idempotent catalog seed: 12 main categories, 40 subcategories, 10 brands, 6 units, and 250 products.
- Flutter home, category hierarchy, product listing, filters, favorites, and product details.
- Admin product management with secure BFF proxy, create, edit, archive, search, and pagination.
- `CatalogExpansion` migration and catalog service tests.

## [0.2.0] — 2026-07-12 — Milestone 2: Identity and Company Verification (20%)

### Added
- OTP login, email/password login, company registration, document verification, JWT access/refresh rotation, logout, and tenant status flows.
- Secure Flutter token storage with automatic single-retry refresh behavior.
- Flutter screens for login, OTP, company registration, documents, and review states.
- Admin login BFF with HttpOnly cookies, role gate, responsive sidebar, dashboard KPIs, charts, and recent orders.
- Seeded platform roles and permissions plus super-admin development account.
- 9 service-level Auth tests and Flutter navigation coverage.

### Security
- OTP request/attempt limits, auth rate limiting, refresh-token reuse detection, tenant-isolated documents, file type and size validation.
- SQLite runtime journals removed from source control.

## [0.1.0] — 2026-07-12 — Milestone 1: Foundation (10%)

### Added
- Monorepo structure: `apps/` (api, admin_web, client_flutter), `packages/`, `infrastructure/`, `docs/`, `seed/`.
- **API** (ASP.NET Core / .NET 10): EF Core + SQLite, JWT auth wiring, Serilog, Swagger, CORS, ProblemDetails, `/health` with DB check.
- **Domain foundation**: BaseEntity/TenantEntity (Guid PK, audit fields, soft delete, row version), Identity module (Tenant, Company, Branch, Documents, User, Role, Permission, OTP, RefreshToken, AuditLog), Catalog module (Category, Brand, Unit, Product, Images, QuantityPriceTiers, Attributes, CompanyProductPrice, Favorite, RecentlyViewed).
- Multi-tenancy: tenant provider from JWT claims + global query filters (tenant isolation + soft delete).
- Initial EF Core migration `InitialFoundation`; auto-migrate in development.
- **Admin Web** (Next.js 16 + TypeScript): Arabic RTL root layout, Cairo font, design-token CSS variables.
- **Client** (Flutter 3.32): theme from design tokens, GoRouter, Splash screen (screen 7), Arabic RTL localization.
- **Design tokens** (`packages/design_tokens/tokens.json`): exact palette extracted from the design PDF (vector fills + pixel sampling), typography scale, spacing, radii, shadows, breakpoints.
- Screen coverage matrix scaffold: 756 screens tracked in `docs/screen-coverage-matrix.csv` + generator script.
- CI (GitHub Actions): API build, Admin lint+build, Flutter analyze+test.
- Docs: README, assumptions log, milestone report.

### Removed
- Legacy FastAPI prototype (recoverable at commit `9308ab8`).
