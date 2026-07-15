# End-to-end release scenarios

These scenarios are the manual acceptance contract for a staging candidate. Automated coverage exists for the platform smoke path; business journeys remain a release checklist until a device/browser E2E harness is added.

## Platform smoke (automated)

- Liveness and database readiness return HTTP 200.
- Security headers are present.
- Anonymous access to an admin endpoint returns HTTP 401.
- A controlled administrator can sign in.
- Catalog categories can be read.
- The monitoring workspace reports database health and background queues.
- The admin login page is reachable through the deployed route.

Run:

```powershell
$password = Read-Host "Release account password" -AsSecureString
./infrastructure/scripts/release-smoke.ps1 `
  -ApiBaseUrl https://api.example.com `
  -AdminBaseUrl https://admin.example.com `
  -AdminEmail release-smoke@example.com `
  -AdminPassword $password
```

## Core customer journeys

1. Register a company, upload valid documents, verify/reject it from admin, then sign in as an approved company user.
2. Browse/search/filter the catalog, see contract/company pricing, add standard and customized products, and submit checkout.
3. Create an approval policy, submit an over-limit purchase, approve/reject it, and verify the audit trail.
4. Create an RFQ, compare responses, negotiate a version, accept it, and convert it to an order.
5. Track an order through fulfillment and delivery; exercise proof of delivery and failed/rescheduled delivery.
6. Create a return, inspect it, apply a disposition, and verify the linked financial/customer-service records.

## Core admin journeys

1. Process an order from intake through shipment, invoice, refund, archive, and reorder paths.
2. Create and publish a product with inventory, company pricing, variants, related items, and media.
3. Run supplier PO, partial receipt, return, invoice, and three-way match.
4. Operate a CRM company through classification, assignment, tasks, meeting, contract, credit, and suspension/reactivation.
5. Configure an integration using a test provider, run it, observe sanitized logs, force a failure, retry it, and disable it.
6. Generate and download XLSX/PDF reports; schedule a report and verify run history.
7. Review health, queues, security events, backup integrity, release version, and feature flags.

## Cross-cutting assertions

- A user from tenant A cannot read or mutate tenant B data, including by changing IDs in requests.
- Every privileged mutation requires the correct permission and creates an audit record.
- Refresh, logout, suspension, lockout, 2FA, and expired-session behavior work as designed.
- Duplicate submits and payment retries are idempotent where documented.
- Arabic RTL layout remains usable at mobile, tablet, and desktop sizes.
- Empty, loading, failure, offline, permission-denied, and retry states are understandable and recoverable.
- Uploaded files reject unsupported type/size and cannot be fetched outside their authorized scope.

