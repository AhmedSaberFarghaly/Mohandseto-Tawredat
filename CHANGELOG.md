# Changelog

All notable changes to Mohandseto Tawredat are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/) and Semantic Versioning per milestone (v0.1.0 → v1.0.0).

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
