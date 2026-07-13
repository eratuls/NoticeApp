# NoticeSaaS — Day 5 (Clients)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 5 done when

- [x] Clients list API with search + module filter
- [x] Add Client API with sync frequency + encrypted portal credentials
- [x] Angular Income Tax clients page + Add Client form
- [x] Demo client seed includes sync schedule + protected password

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

Open http://localhost:4200 → Clients.

## Next — Day 6

Client notice list (tabs) + notice detail (manual upload first).
