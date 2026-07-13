# NoticeSaaS — Day 7 (Reminders & notifications)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 7 done when

- [x] Reminders API with Pending/Done tabs, priority + search filters
- [x] Create reminder from notice detail (creates in-app notification)
- [x] Notifications API with unread badge, mark read / mark all read
- [x] Angular Reminders page + shell Alerts panel
- [x] Demo reminders and notifications seeded

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

Open http://localhost:4200 → Reminders, or notice detail → Set reminder · Alerts in the top strip.

## Next — Day 8

Income Tax sync worker (password-only portal accounts).
