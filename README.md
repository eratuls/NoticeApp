# NoticeSaaS - Day 12 (Phase 1 wrap-up)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 12 done when

- [x] Notice PDF + reply uploads on notice detail (downloadable attachments)
- [x] Assign notice to a team member
- [x] Calendar shell stub; Manual / Case Status seed + Add manual notice
- [x] Tests for assign + attachment upload/download

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

Open a notice → upload PDF/reply, assign a team member. Calendar is a Phase 1 stub in the shell.

## Next - Day 13

Phase 1.5 / rollout prep: live portal hardening, Azure packaging, or optional AI analyzer.
