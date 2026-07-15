# Test strategy

## Release gate

Every release candidate must pass the same five layers:

1. API compile and automated tests on a clean database.
2. Admin lint, dependency audit, and production build.
3. Flutter static analysis and widget/service tests.
4. Container build plus Compose configuration validation in CI.
5. Authenticated smoke testing against the deployed candidate.

The CI definition is `.github/workflows/ci.yml`. A release must not be tagged while any required job is red.

## Test layers

| Layer | Scope | Command |
|---|---|---|
| API unit/service | Business rules, authorization, tenancy, state transitions | `dotnet test apps/api.Tests/Mohandseto.Api.Tests.csproj -c Release` |
| Migration integrity | Apply the complete EF migration chain to an empty SQLite database | Included in API tests |
| API E2E | Health, security headers, login, catalog, monitoring, role rejection | Included in API tests through `WebApplicationFactory` |
| Admin static/build | Type safety, lint rules, server/client boundaries, production bundle | `npm run lint && npm run build` |
| Flutter | Analysis, navigation, widgets, repositories, core journeys | `flutter analyze && flutter test` |
| Release smoke | Live deployed API and admin BFF | `infrastructure/scripts/release-smoke.ps1` |

## Test data

- Automated API E2E tests create a unique temporary SQLite database and Data Protection key directory.
- Development seed data is deterministic and must never be enabled in production.
- Staging release accounts must be non-personal, least-privilege, and stored in the deployment secret store.
- Test artifacts, tokens, passwords, databases, and logs containing customer data are not committed.

## Non-functional checks

- Security: fail-fast production configuration, dependency vulnerability scan, secret scan, authorization tests, file limits, rate limits, and response headers.
- Performance: inspect API latency/error/queue metrics in the monitoring workspace and run representative peak traffic before GA. Formal load-test thresholds require production-sized infrastructure and data.
- Accessibility: keyboard navigation, visible focus, semantic labels, contrast, text scaling, RTL, and screen-reader smoke checks on critical journeys.
- Compatibility: current Chrome/Edge/Safari, supported Android/iOS targets, desktop/mobile admin breakpoints, slow and interrupted network paths.

## Defect policy

- P0/P1 security, data-loss, authorization, payment, or tenant-isolation defects block release.
- P2 defects block release when they affect a core journey without a safe workaround.
- Known lower-severity defects require an owner, documented impact, and target release.
