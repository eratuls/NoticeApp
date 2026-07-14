# NoticeSaaS - Day 14 (Azure deploy skeleton)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 14 done when

- [x] Bicep skeleton for Container Apps + Azure SQL + Blob Storage (`infra/`)
- [x] Attachments: Local (dev) vs Azure Blob (prod) via `Storage:Provider`
- [x] Deploy docs for secrets (JWT, SQL, DataProtection, Blob)

### Auth

| Email | Password |
|-------|----------|
| `admin@noticesaas.local` | `Admin@12345` |

### Attachment storage

| Environment | `Storage:Provider` | Notes |
|-------------|--------------------|--------|
| Development | `Local` | Files under `Storage:NoticeAttachmentsPath` (or temp dir) |
| Production | `AzureBlob` | Uses `Storage:AzureBlob:ConnectionString` + `ContainerName` |

```text
Storage__Provider=AzureBlob
Storage__AzureBlob__ConnectionString=...
Storage__AzureBlob__ContainerName=notice-attachments
```

### Azure deploy (skeleton)

```powershell
az group create -n rg-noticesaas -l eastus

az deployment group create `
  -g rg-noticesaas `
  -f infra/main.bicep `
  -p infra/main.bicepparam `
  -p sqlAdminPassword='<strong-password>' `
     jwtSigningKey='<32+-char-secret>' `
     apiImage='<acr>/noticesaas-api:tag' `
     webImage='<acr>/noticesaas-web:tag'
```

**Secrets to inject (never commit):**

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings__Default` | Azure SQL |
| `Auth__Jwt__SigningKey` | JWT (≥32 chars) |
| `Storage__AzureBlob__ConnectionString` | Blob attachments |
| `DataProtection__KeysPath` / shared volume / blob | ASP.NET DataProtection key ring |

Wire CORS `Cors__AngularOrigins__0` to the web Container App HTTPS URL after first deploy.

### Local packaging

```powershell
docker compose -f docker-compose.yml -f docker-compose.app.yml up -d --build
```

## Next - Day 15

Live portal hardening or optional AI notice analyzer (Phase 1.5).
