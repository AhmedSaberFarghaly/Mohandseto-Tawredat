# Milestone 102 — Reporting Engine (Screens 671–699)

## Delivered

- A central Arabic RTL reports workspace matching the supplied reporting designs.
- 22 built-in reports spanning sales, orders, RFQs, companies, products, inventory, procurement, finance, contracts, printing, delivery, returns, support and staff performance.
- KPIs, monthly trends, business breakdowns, detailed rows and shared date/company/warehouse/staff/status/value/search filters.
- A custom report builder with eight validated sources, field selection, grouping, chart mode and live preview.
- Persisted favorite templates, daily/weekly/monthly schedules, multiple recipients, Excel/PDF formats, execution history and background processing.
- Native XLSX (OpenXML package) and PDF generation with browser download flows.
- Role-protected admin API access, write restrictions for auditors and mutation audit records.

## Persistence

Migration `AddReportingEngine` adds:

- `SavedReports` for report definitions and schedules.
- `ReportRuns` for durable processing history, success/failure state and delivery metadata.

## Verification

- Every one of the 22 built-in report codes executes against the real EF Core SQLite schema.
- Custom preview, filtering, grouping, template persistence, scheduling, background delivery, XLSX signature and PDF signature are automated.
- Backend suite: 71 passed.
- Admin web: ESLint passed; Next.js production build passed with `/dashboard/reports` included.
