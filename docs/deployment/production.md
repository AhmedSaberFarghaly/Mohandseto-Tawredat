# Production deployment and rollback

## Supported v1 topology

The checked-in EF migration chain targets SQLite. The supported initial topology is therefore **one API writer instance** using an encrypted persistent volume, one or more stateless admin web instances, and a TLS reverse proxy. Do not scale the API horizontally against the same SQLite file.

SQL Server remains a deliberate future cutover: create provider-specific migrations, rehearse data conversion, validate all tests against SQL Server, and prepare rollback before changing the production provider.

## Required inputs

- Public API hostname and HTTPS admin origin.
- A secret-store supplied JWT signing key (48+ random characters), provider credentials, SMTP/SMS/payment credentials, and controlled smoke account.
- Encrypted persistent storage for `/var/lib/mohandseto/data` and `/var/lib/mohandseto/keys`.
- Off-host, encrypted, retention-controlled backups and tested restore access.
- DNS/TLS reverse proxy using `infrastructure/nginx/mohandseto.conf.example` as a starting point.
- The trusted proxy IP as seen by the API. The supplied Compose network defaults to `172.31.50.0/24`, whose gateway/proxy address is `172.31.50.1`; change both environment values if that subnet conflicts with the host.
- Named release owner, database owner, security approver, rollback owner, and maintenance window.
- Google Cloud and Microsoft Entra registrations configured exactly as described in [OAuth setup](oauth.md), including the iOS reversed client-ID scheme at build time.
- Android release signing supplied through ignored `apps/client_flutter/android/key.properties` (template included) or `MOHANDSETO_ANDROID_*` secret environment variables. Release builds fail rather than fall back to a debug key.

## Pre-deploy gate

1. All CI jobs pass for the exact commit; no high/critical vulnerability or secret finding is waived silently.
2. The staging candidate passes automated smoke and critical manual E2E scenarios.
3. Take and verify an off-host database backup plus the matching Data Protection keys. Losing the keys can make protected integration credentials unreadable.
4. Confirm free disk space, alert routing, log retention, provider sandbox/live modes, feature flags, and maintenance communication.
5. Complete Google and Microsoft sign-in, cancellation, first-time linking, repeat login, 2FA and revoked-user tests on physical Android/iOS devices.
6. Validate configuration without printing secrets:

   ```bash
   docker compose --env-file /secure/path/production.env \
     -f infrastructure/docker/compose.production.yaml config --quiet
   ```

## Deploy

```bash
docker compose --env-file /secure/path/production.env \
  -f infrastructure/docker/compose.production.yaml build --pull
docker compose --env-file /secure/path/production.env \
  -f infrastructure/docker/compose.production.yaml up -d
```

The API fails fast if production uses an unsafe JWT key, wildcard host, loopback/non-HTTPS CORS, enabled seed, missing connection string, or non-persistent Data Protection path. Startup migration is enabled for this single-instance Compose topology; never start competing migrators.

After startup, check `/health/live`, `/health/ready`, run the authenticated smoke script, and watch error rate, latency, queues, database/storage health, security events, and business transactions through the observation window.

## Rollback

1. Stop inbound traffic and background work if data integrity may be affected.
2. Capture logs and a forensic copy; do not overwrite the last known-good backup.
3. If the migration is backward-compatible, redeploy the previous immutable image digest.
4. If data/schema rollback is required, stop the API, restore the database **and matching Data Protection keys** into isolated volumes, verify hash/integrity, then start the previous release.
5. Run smoke tests before restoring traffic and document the incident/change record.

Never edit a production SQLite file while the API is running. A rollback is incomplete until authentication, catalog, admin monitoring, background queues, and a representative write all pass.
