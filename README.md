# NoticeSaaS - Day 16 (Azure Key Vault secrets)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 16 done when

- [x] Key Vault config placeholders for JWT, SQL, Blob (`KeyVault:Enabled` / `VaultUri`)
- [x] Document local vs Azure secret loading
- [x] Local storage + emulators work without Key Vault (`Enabled: false`)

### Auth

| Email | Password |
|-------|----------|
| `admin@noticesaas.local` | `Admin@12345` |

### Secrets: local vs Azure

| Mode | How |
|------|-----|
| **Local / Docker** | `KeyVault:Enabled=false` (default). Use `appsettings.Development.json`, env vars, or user-secrets. |
| **Azure** | Set `KeyVault__Enabled=true` and `KeyVault__VaultUri=https://{vault}.vault.azure.net/`. App uses `DefaultAzureCredential` (managed identity / Azure CLI). |

Store nested config as Key Vault secret names with `--`:

| Secret name | Config key |
|-------------|------------|
| `ConnectionStrings--Default` | SQL |
| `Auth--Jwt--SigningKey` | JWT (≥32 chars) |
| `Storage--AzureBlob--ConnectionString` | Blob attachments |

`infra/main.bicep` provisions the vault and those secrets. Container Apps still receive env `secretRef` values so deploy works before managed identity RBAC is wired; flip `KeyVault__Enabled=true` after granting the app **Key Vault Secrets User**.

### Packaging

```powershell
docker compose -f docker-compose.yml -f docker-compose.app.yml up -d --build
```

## Next - Day 17

Optional AI notice analyzer (Phase 1.5) or production smoke checklist.
