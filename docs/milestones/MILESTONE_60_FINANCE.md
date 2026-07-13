# Milestone 60 — Invoices & Payments

Date: 2026-07-13

## Scope completed

- Client screens 256–279 are implemented in Flutter as an invoice center, financial dashboard and invoice detail/payment journey.
- New checkout and RFQ orders issue immutable tax invoices with item, tax, discount, shipping and payment snapshots.
- Invoice PDF, share summary, electronic QR payload and valid XLSX consolidated export are implemented.
- Outstanding, overdue, due-soon, payment history and due-calendar views are calculated from verified data.
- Bank transfer initiation issues a unique payment reference and bank instructions; secure receipt upload enters role-protected verification.
- Credit limit, utilization, available balance and increase applications with finance decisions are implemented.

## Verification

- Backend: 33 tests passed, including invoice issuance, PDF/XLSX, bank transfer, payment verification, credit approval and tenant isolation.
- Flutter: 17 tests passed; static analysis has no issues.
- EF Core: `FinanceWorkflow` is migration 14 and applies successfully to a fresh database.

## Gate

Invoices and payments are complete. Budgets/cost centers and company account remain in M6.
