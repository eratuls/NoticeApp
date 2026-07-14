# NoticeSaaS - Day 11 (Phase 1 polish: Team + Master)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 11 done when

- [x] Dashboard shows clients, team, notices, and New/Ongoing/Closed/Overdue buckets
- [x] Team list + Add Member (department Income Tax / GST / TDS / Accounting)
- [x] Master Departments / Designations CRUD + Roles list
- [x] Tests for master CRUD and team add path

### Auth

| Email | Password |
|-------|----------|
| `admin@noticesaas.local` | `Admin@12345` |

### Seeded master data

| Type | Values |
|------|--------|
| Departments | Accounting, GST, Income Tax, TDS |
| Designations | Partner, Manager, Associate, Article Assistant |

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

Open http://localhost:4200 → **Team** to add members, **Master** for departments/designations.

## Next - Day 12

Phase 1 wrap-up: acceptance checklist gaps (notice detail polish, reminders/calendar stubs), prep for Phase 1.5 or rollout.
