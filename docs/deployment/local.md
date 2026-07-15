# Local deployment

## Docker Compose (recommended)

Requirements: Docker Desktop with Compose v2.

```bash
docker compose up --build
```

- Admin: `http://localhost:3000`
- API: `http://localhost:5199`
- API health: `http://localhost:5199/health/ready`

The `api_data` and `data_protection_keys` named volumes preserve the local database and protected integration settings. Stop with `docker compose down`. Do not add `-v` unless you intentionally want to delete local data and keys.

## Native development

```bash
dotnet run --project apps/api/Mohandseto.Api.csproj
```

The launch profile exposes the API at `http://localhost:5247` and applies/seeds the development SQLite database.

In a second terminal:

```bash
cd apps/admin_web
npm ci
npm run dev
```

If the API is not at the admin default, set server-only `API_BASE_URL` before starting Next.js. For Flutter, run `flutter pub get` then `flutter run` from `apps/client_flutter` and set its API base URL for the target emulator/device.

Never reuse development credentials, seed accounts, SQLite files, or Data Protection keys in staging or production.
