# NoticeSaaS - Day 10 (OTP-assisted portal sync)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

## Day 10 done when

- [x] Detect vault/OTP-required portal accounts during sync login
- [x] Pause sync job awaiting user OTP; API + UI handoff to submit OTP
- [x] Resume worker after OTP; complete notice upsert or fail cleanly on timeout
- [x] Tests for OTP pause / resume path (mock portal)

### Auth

| Email | Password |
|-------|----------|
| `admin@noticesaas.local` | `Admin@12345` |

### OTP-assisted sync (mock portal)

| Portal password | Behavior |
|-----------------|----------|
| Normal password | Unattended sync (Day 8 path) |
| `vault-otp` | Job pauses as `AwaitingOtp`; submit OTP `123456` in UI to resume |

Timeout: 5 minutes without OTP → job fails.

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

Open http://localhost:4200 → Clients → add a client with portal password `vault-otp` → Sync now → enter OTP `123456`.

## Next - Day 11

Phase 1 polish from acceptance checklist (dashboard buckets, team, master data gaps).
