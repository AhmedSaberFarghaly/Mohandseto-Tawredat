# Milestone 62 — Budgets & Cost Centers

Date: 2026-07-13

## Scope completed

- Client screens 280–293 are implemented as company and cost-center budget dashboards.
- Annual totals, monthly spend, used/reserved/available amounts, department allocation and highest-utilization centers are calculated from order snapshots.
- Cost-center detail exposes orders, projects, departments, actual monthly spend, average run rate and year-end forecast.
- Automatic alerts cover 80% warning, 100% exceeded and forecast-over-budget conditions.
- Budget adjustment requests validate increases and duplicate active requests; finance decisions update the center and notify the requester.

## Verification

- Backend: 34 tests passed, including forecast, alerts, adjustment decision and tenant isolation.
- Flutter: 19 tests passed; static analysis has no issues.
- EF Core: `BudgetAnalytics` is migration 15 and applies successfully to a fresh database.

## Gate

Budgets and cost centers are complete. Company account/users and notification/support/settings remain in the client scope.
