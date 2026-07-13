# NoticeSaaS — Day 6 (Notices)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 6 done when

- [x] Client notice list API with kind filter + search
- [x] Notice detail API with status update, timeline, and comments
- [x] Angular client notices page (tabs) + notice detail page
- [x] Demo seed includes Direct Order kind

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

Open http://localhost:4200 → Clients → notice count → notice list / detail.

## Next — Day 7

Reminders and notifications for due / overdue notices.
