# Milestone 57 — Returns & Replacement

Date: 2026-07-13

## Scope completed

- Client screens 236–255 are implemented as a five-step Flutter return wizard and a state-driven return detail journey.
- Eligibility is calculated per delivered order and order item for 30 days, including prior active return quantities.
- Requests support partial quantities, structured reasons, descriptions, secure condition photos, replacement or refund resolution, refund method and pickup address.
- Staff review, approval/rejection, pickup scheduling, driver tracking, receipt and inspection are role protected and fully audited.
- Passed inspections lead to original-payment, bank-transfer or credit-balance refunds, or replacement preparation/shipping/delivery.
- All customer data and evidence are tenant and owner isolated; cancellation and rejected requests release item eligibility.

## Verification

- Backend: 32 tests passed, including full refund lifecycle, rejection and tenant isolation.
- Flutter: 15 tests passed; static analysis has no issues.
- EF Core: `ReturnsWorkflow` is migration 13 and applies successfully to a fresh database.

## Gate

The returns/replacement slice of M6 is complete. Invoices/payments, budgets and account remain.
