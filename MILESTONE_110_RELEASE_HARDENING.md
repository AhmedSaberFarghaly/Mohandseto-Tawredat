# Milestone 110 — v1.0.0 Release Candidate Hardening

Date: 2026-07-15

## Delivered

- Production fail-fast validation for JWT, host/CORS, seeding, persistent Data Protection keys, connection string, and trusted reverse proxy configuration.
- Security headers, HSTS, response compression, forwarded client IP handling, global/auth rate limits, request-size limits, and separate live/readiness health probes.
- Non-root multi-stage API/admin Dockerfiles, local Compose, production Compose with persistent volumes and a deterministic trusted-proxy network, plus an Nginx/TLS starting configuration.
- Automated production configuration tests, full empty-database migration verification, authenticated API E2E test, and a live release smoke script that accepts the password as a `SecureString`.
- CI gates for .NET/Next/Flutter, production dependency audits, secret scanning, container builds, and Compose validation.
- Product, feature, architecture, database, OpenAPI, threat, permissions, QA, E2E, local/staging/production/rollback, and release-checklist documentation.

## Verification evidence

- .NET Release build: 0 warnings, 0 errors.
- Backend: 90/90 tests passed, including all 35 migrations from an empty SQLite database.
- Admin: ESLint passed; Next.js 16.2.10 production build passed for 33 routes.
- Flutter: analysis passed; 23/23 tests passed.
- Live local release smoke: liveness, readiness, headers, anonymous rejection, admin login, catalog, monitoring, and admin web all passed.
- NuGet production dependency scan: no vulnerable packages.
- npm production audit: no high/critical findings; two moderate transitive PostCSS advisories remain documented because the automated forced remedy downgrades Next.js incompatibly.
- Git diff whitespace gate and targeted credential-pattern scan passed.

Docker is not installed on the current workstation, so container execution is intentionally a CI/staging gate; the files were not falsely marked as locally container-tested.

## GA gates outside the repository

- Provide production hosting, DNS/TLS, secret store, provider credentials, controlled smoke account, privacy/legal approval, and licensed commercial visual assets.
- Deploy the exact commit to a production-like staging environment; complete critical business E2E, load/performance, accessibility/device/browser, provider sandbox, alert delivery, and off-host backup/restore evidence.
- Approve the change window and rollback point. Only then create the signed `v1.0.0` tag.

The checked-in v1 database path is SQLite single-writer. SQL Server or horizontal API scale requires a separate provider migration/cutover project and is not represented as complete.

Final screen-matrix reconciliation also replaced stale placeholders with live company/branch selection, persisted default-branch changes, branch/address details, compact profile data, a dedicated recently-viewed rail, and a correct suspended-account recovery state. At this milestone the honest gate was 753/756. Milestone 111 subsequently closed the remaining rows after re-reading the PDF: screen 48 was invoice export—not multi-company switching—and the two social-auth rows now have working provider adapters and secure account-linking flows.
