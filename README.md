# NoticeSaaS - Day 9 (Usage & Limits)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 9 done when

- [x] Usage & Limits API: assessee seats + sync credit quotas for the org
- [x] Sync credit ledger: decrement on successful sync; block when exhausted
- [x] Settings > Usage & Limits UI (meters + remaining credits)
- [x] Seed demo subscription quotas; tests for quota enforcement

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
cd web/notice-saas-web
npm start
```

Open http://localhost:4200 → Usage (or header Sync credits).

## Next - Day 10

OTP-assisted portal sync handoff (or polish from Phase 1 acceptance checklist).
