# Milestone 99 — Accounts, Returns & Customer Service

Date: 2026-07-15  
Design coverage: admin screens 600–639  
Routes: `/dashboard/accounting`, `/dashboard/customer-service`

## Delivered

- Accounting dashboard, customer invoices, tax-invoice detail/PDF, bank transfers and matching, receivables, aging, statements, credit/debit notes, refunds, expenses, product/company profitability, sales tax, Excel export and validated financial-period close.
- Cross-company returns queue and detail workspace with reason/photo review, decisions, pickup, inspection, refund/replacement progress, restock, disposal and posted credit-note dispositions.
- Support ticket queue and chat with staff assignment, SLA deadlines, escalation, reply templates, status control, team ratings and issue-type reporting.
- New durable accounting entries, financial periods, SLA policies, reply templates, return dispositions and ticket deadline/escalation fields.
- Seeded 32 SLA policies and 5 Arabic reply templates.

## Verification

- Migration: `AccountingCustomerServiceOperations` (migration 29).
- Backend: 60/60 tests passed, including the accounting close cycle and customer-service/restock cycle.
- Admin: ESLint clean and Next.js 16 production build successful.
- Authenticated HTTP smoke: login, both dashboards and both BFF APIs returned 200; 32 SLA policies and 5 reply templates loaded.
- Screen coverage rows 600–639 are marked `Implemented / Automated`.

## Operational safeguards

- Duplicate order invoices, overpayments and duplicate period closes are rejected.
- Financial close is blocked while draft entries or unmatched transfers exist.
- Return files remain authenticated and path constrained.
- Restocks update warehouse balance and write an immutable inventory movement; disposals and credit notes remain auditable.
- SLA deadlines are stamped when customer tickets are created and are measured against real response/resolution timestamps.
