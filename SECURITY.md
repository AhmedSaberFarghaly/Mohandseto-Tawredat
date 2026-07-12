# Security Policy — Mohandseto Tawredat

## Principles

- **Security by design**: authorization at the API layer (never UI-only), tenant isolation via global query filters, audit logging for sensitive actions.
- **Secrets are never committed.** All credentials come from environment variables (`Jwt__Key`, connection strings, provider keys). `appsettings.json` contains development-only placeholder values.
- Passwords hashed with BCrypt; OTP codes stored hashed with expiry + attempt limits; JWT access tokens are short-lived with rotating refresh tokens.

## Reporting a Vulnerability

Open a private security advisory on GitHub or contact the repository owner directly. Do not open public issues for vulnerabilities.

## Planned hardening per milestone

- Rate limiting & brute-force protection (Milestone 2 — auth).
- 2FA for admin console (Milestone 2).
- File upload validation (MIME/type/size) and signed URLs (Milestone 4).
- Full threat model & permissions matrix in `docs/security/` (built progressively; finalized Milestone 10).
