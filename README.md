# NoticeSaaS — Day 1 scaffold

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Solution layout

```text
NoticeSaaS.sln
├── src/
│   ├── NoticeSaaS.Api              # HTTP API
│   ├── NoticeSaaS.Application      # Use cases (Day 2+)
│   ├── NoticeSaaS.Domain           # Entities (Day 2+)
│   ├── NoticeSaaS.Infrastructure   # EF Core, Blob, Key Vault (Day 2+)
│   └── NoticeSaaS.Workers          # Sync jobs (Week 3)
├── web/
│   └── notice-saas-web             # Angular
└── tests/
    └── NoticeSaaS.UnitTests
```

## Day 1 done when

- [x] Solution builds
- [x] `GET /health` and `GET /api/health` return OK
- [x] Application Insights package wired (set connection string when Azure resource exists)
- [x] Angular app calls health API and shows status
- [x] CORS allows `http://localhost:4200`

## Run locally

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

## Next — Day 2

EF Core + Azure SQL: `Organizations`, `Users`, `OrganizationMembers`, `Roles` + seed admin.
