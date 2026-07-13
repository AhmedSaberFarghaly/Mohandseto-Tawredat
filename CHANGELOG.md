# Changelog

All notable changes to Mohandseto Tawredat are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/) and Semantic Versioning per milestone (v0.1.0 → v1.0.0).

## [Unreleased] — Milestone 7 in progress

### Added
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
