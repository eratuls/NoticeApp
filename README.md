# NoticeSaaS - Day 13 (Azure packaging + CI)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 13 done when

- [x] Multi-stage Dockerfiles for API and Angular web
- [x] Compose overlay to run API + web against local SQL
- [x] GitHub Actions CI (restore, build, test)
- [x] API applies migrations on startup (not only Development)

### Auth

| Email | Password |
|-------|----------|
| `admin@noticesaas.local` | `Admin@12345` |

### Packaging

```powershell
# SQL only (dev)
docker compose up -d

# Full stack locally
docker compose -f docker-compose.yml -f docker-compose.app.yml up -d --build
```

- API: http://localhost:8080/health
- Web: http://localhost:8088 (proxies `/api` to the API container)

### Run (host / without containers)

```powershell
docker compose up -d
cd src/NoticeSaaS.Api
dotnet run
```

```powershell
cd web/notice-saas-web
npm start
```

## Next - Day 14

Optional: Azure Container Apps / App Service ARM-Bicep skeleton, blob storage for attachments, or live portal hardening.
