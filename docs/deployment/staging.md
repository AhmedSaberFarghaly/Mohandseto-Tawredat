# Staging deployment

Staging must mirror production topology, HTTPS routing, persistent volumes, secret injection, and backup policy. Use isolated accounts and provider sandboxes; never copy live customer data without an approved anonymization process.

## Procedure

1. Provision a Linux host with Docker Engine, Compose v2, encrypted persistent storage, DNS, TLS, monitoring, and off-host backup storage.
2. Copy `infrastructure/docker/production.env.example` to a protected deployment secret/environment source; generate a unique JWT key of at least 48 random characters.
3. Validate the release SHA and run all CI jobs.
4. Render and validate configuration:

   ```bash
   docker compose --env-file /secure/path/staging.env \
     -f infrastructure/docker/compose.production.yaml config
   ```

5. Back up the existing database and Data Protection key volume.
6. Build and start the candidate. Only one API replica may run migrations or write the current SQLite database.
7. Route TLS traffic through the reverse proxy and keep Compose ports bound to loopback.
8. Run `release-smoke.ps1`, then execute the critical scenarios in `docs/qa/e2e-scenarios.md`.
9. Verify monitoring, audit events, background queues, backup integrity, and restoration on an isolated copy.
10. Record the image digest, Git SHA, migration, approver, evidence, and rollback point.

Staging approval does not authorize production deployment; production uses a separate change record and secrets.

