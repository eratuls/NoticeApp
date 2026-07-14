# NoticeSaaS - Day 15 (Portal hardening)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 15 done when

- [x] Harden mock/live portal client boundaries (timeouts, retry, clearer errors)
- [x] Audit: no plaintext portal passwords in logs or sync job payloads
- [x] Sync job UX: surface portal errors cleanly in Clients / Notices

### Auth

| Email | Password |
|-------|----------|
| `admin@noticesaas.local` | `Admin@12345` |

### Mock portal hardening passwords

| Portal password | Behavior |
|-----------------|----------|
| Normal | Unattended sync |
| `vault-otp` | AwaitingOtp → OTP `123456` |
| `transient-once` | First fetch fails transiently, retry succeeds |
| `portal-timeout` | Sync fails with safe timeout/unavailable message |
| `wrong-password` | Rejected at add-client / login |

Worker uses a 30s portal call timeout and up to 3 attempts on transient failures. Sync job errors never echo passwords or OTPs.

### Attachment storage

- Dev: `Storage:Provider=Local`
- Prod: `Storage:Provider=AzureBlob` (+ connection string / container)

### Packaging

```powershell
docker compose -f docker-compose.yml -f docker-compose.app.yml up -d --build
```

## Next - Day 16

Optional AI notice analyzer (Phase 1.5) or Azure Key Vault for secrets.
