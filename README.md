# NoticeSaaS — Day 8 (Income Tax sync worker)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 8 done when

- [x] `SyncJob` / `SyncJobLog` entities + migration; enqueue due clients by `NextSyncAtUtc`
- [x] Trigger sync from Clients UI / API (password-only portal accounts)
- [x] Worker decrypts `PortalCredential` in-process; mock password-only portal adapter (Playwright-ready interface)
- [x] Fetch notices, upsert into `Notices` by `DocumentReferenceId`; update `LastSyncAtUtc` / `NextSyncAtUtc`
- [x] Sync status visible on client list (Succeeded / Failed + error)
- [x] Endpoint tests for enqueue + notice upsert / idempotent dedupe

### Auth

| Email | Password |
|-------|----------|
| `admin@noticesaas.local` | `Admin@12345` |

### Run

```powershell
docker compose up -d
cd src/NoticeSaaS.Api
dotnet run
```

```powershell
cd src/NoticeSaaS.Workers
dotnet run
```

```powershell
cd web/notice-saas-web
npm start
```

Open http://localhost:4200 → Clients → **Sync now** on a password-only account.

## Next — Day 9

Usage & Limits (sync credits / assessee quotas).
