# NoticeSaaS — Day 3 (Auth + shell)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Solution layout

```text
NoticeSaaS.sln
├── src/
│   ├── NoticeSaaS.Api              # HTTP API
│   ├── NoticeSaaS.Application      # Use cases / contracts
│   ├── NoticeSaaS.Domain           # Entities
│   ├── NoticeSaaS.Infrastructure   # EF Core, auth, JWT
│   └── NoticeSaaS.Workers          # Sync jobs (Week 3)
├── web/
│   └── notice-saas-web             # Angular
├── docker-compose.yml              # Local SQL Server
└── tests/
    └── NoticeSaaS.UnitTests
```

## Day 1–2 done when

- [x] Solution builds, health endpoints, Angular ↔ API
- [x] Docker SQL + EF Core tenancy + seed admin

## Day 3 done when

- [x] Login API with JWT
- [x] Single active session + force logout conflict
- [x] Session idle timer (default 10 minutes)
- [x] Angular login, auth guard, shell + session countdown

### Seed admin (Development)

| Field | Value |
|-------|--------|
| Email | `admin@noticesaas.local` |
| Password | `Admin@12345` |
| Organization | NoticeSaaS Demo (Owner) |

### Auth API

| Method | Path | Notes |
|--------|------|--------|
| POST | `/api/auth/login` | Body: `{ email, password, forceLogout }` |
| POST | `/api/auth/logout` | Bearer required |
| GET | `/api/auth/session` | Remaining TTL + user |
| GET | `/api/auth/me` | Current user |

`409 SESSION_ACTIVE` when another session exists and `forceLogout` is false.

## Local SQL Server (Docker)

```powershell
cd d:\NoticeApp
docker compose up -d
```

## Open in Visual Studio

Open `NoticeSaaS.sln` — **src** (.NET) and **web → notice-saas-web** (Angular).

1. Right-click solution → **Configure Startup Projects…** → **Multiple startup projects**
2. Set **NoticeSaaS.Api** and **notice-saas-web** to **Start**
3. On the toolbar profile for the API, choose **http** (port **5166**) — not IIS Express
4. Start (F5), then open **http://localhost:4200** (Angular). Do not use `https://localhost:44353` — that is the API root and returns 404 by design.

## Run locally (CLI)

```powershell
docker compose up -d
cd src/NoticeSaaS.Api
dotnet run
```

```powershell
cd web/notice-saas-web
npm start
```

Open http://localhost:4200 → login → dashboard shell.

## Next — Day 4

Dashboard summary cards + notice task buckets (API + UI).
