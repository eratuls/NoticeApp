# NoticeSaaS — Day 2 (EF Core tenancy)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Solution layout

```text
NoticeSaaS.sln
├── src/
│   ├── NoticeSaaS.Api              # HTTP API
│   ├── NoticeSaaS.Application      # Use cases
│   ├── NoticeSaaS.Domain           # Entities
│   ├── NoticeSaaS.Infrastructure   # EF Core, Blob, Key Vault
│   └── NoticeSaaS.Workers          # Sync jobs (Week 3)
├── web/
│   └── notice-saas-web             # Angular
├── docker-compose.yml              # Local SQL Server
└── tests/
    └── NoticeSaaS.UnitTests
```

## Day 1 done when

- [x] Solution builds
- [x] `GET /health` and `GET /api/health` return OK
- [x] Application Insights package wired (set connection string when Azure resource exists)
- [x] Angular app calls health API and shows status
- [x] CORS allows `http://localhost:4200`

## Day 2 done when

- [x] Local SQL Server via Docker Compose
- [x] `ConnectionStrings:Default` in Development appsettings
- [x] EF Core entities: Organizations, Users, OrganizationMembers, Roles
- [x] Migration applied on API startup (Development)
- [x] Seeded system roles + demo org + admin user

### Seed admin (Development)

| Field | Value |
|-------|--------|
| Email | `admin@noticesaas.local` |
| Password | `Admin@12345` |
| Organization | NoticeSaaS Demo (Owner) |

## Local SQL Server (Docker)

```powershell
cd d:\NoticeApp
docker compose up -d
```

Development connection string is in `appsettings.Development.json` (`ConnectionStrings:Default`).

| Setting | Value |
|---------|--------|
| Server | `localhost,1433` |
| Database | `NoticeSaaS` |
| User | `sa` |
| Password | matches `MSSQL_SA_PASSWORD` in `docker-compose.yml` |

Production/staging: leave `ConnectionStrings:Default` empty in `appsettings.json` and set it via Azure App Settings or Key Vault.

### EF migrations

```powershell
dotnet ef migrations add <Name> --project src/NoticeSaaS.Infrastructure --startup-project src/NoticeSaaS.Api
dotnet ef database update --project src/NoticeSaaS.Infrastructure --startup-project src/NoticeSaaS.Api
```

## Open in Visual Studio

Open `NoticeSaaS.sln` — Solution Explorer should show **src** (.NET) and **web** → **notice-saas-web** (Angular).

Requires Visual Studio 2022 with the **Node.js development** / **JavaScript and TypeScript** workload, plus Node.js installed.

To run API + web together:

1. Right-click the **solution** → **Configure Startup Projects…**
2. Choose **Multiple startup projects**
3. Set **NoticeSaaS.Api** and **notice-saas-web** to **Start**
4. Press **F5** (or Ctrl+F5)

First time: if prompted, restore npm packages on `notice-saas-web` (`npm install`).

If the web project is missing or unloadable, install/repair the Node.js workload and reopen the solution.

**API** (https://localhost:7238 or http://localhost:5166):

```powershell
cd src/NoticeSaaS.Api
dotnet run
```

**Angular:**

```powershell
cd web/notice-saas-web
npm start
```

Open http://localhost:4200 — you should see **API OK**.

## Application Insights

In `src/NoticeSaaS.Api/appsettings.Development.json` (or Azure App Settings):

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
  }
}
```

Leave empty for local-only logging until the Azure resource is created.

## Next — Day 3

Auth (login), single-session rules, and Angular main shell.
