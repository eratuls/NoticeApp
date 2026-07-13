# NoticeSaaS — Day 4 (Dashboard)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 4 done when

- [x] `Client` + `Notice` entities and migration
- [x] `GET /api/v1/dashboard/summary` (module + period filters)
- [x] Task buckets: New / Ongoing / Closed / Overdue
- [x] Angular dashboard wired to live summary data
- [x] Demo client + 21 Income Tax notices seeded

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

Open http://localhost:4200 → sign in → dashboard overview.

## Next — Day 5

Clients list + Add Client (credentials + sync type).
