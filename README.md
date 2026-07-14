# NoticeSaaS - Day 17 (Production smoke checklist)

Income Tax notice SaaS: **Angular** web + **ASP.NET Core** API + workers, Azure-ready.

**Phase 1 status:** complete (Income Tax module MVP). Optional Phase 1.5 follows (AI analyzer).

## Day 17 done when

- [x] End-to-end smoke checklist documented (auth, clients, sync, OTP, attachments, usage)
- [x] Health / packaging sanity notes for Docker full stack
- [x] Phase 1 acceptance mapped to checklist
- [x] Automated API smoke script (`scripts/smoke-phase1.ps1`)

### Auth

| Email | Password |
|-------|----------|
| `admin@noticesaas.local` | `Admin@12345` |

### Secrets

- Local: `KeyVault:Enabled=false`
- Azure: `KeyVault__Enabled=true` + `KeyVault__VaultUri=...`

---

## Phase 1 acceptance → smoke map

| Acceptance criterion | Where to verify |
|----------------------|-----------------|
| Dashboard clients / team / notices + New/Ongoing/Closed/Overdue | UI `/dashboard` |
| Add Income Tax client (category, sync type, username, password) | UI `/clients` → Add client |
| Notice tabs Notices / Direct Orders / Manual / Case Status | UI client notices page |
| Notice detail: metadata, PDF, reply upload, status, timeline, comments | UI `/notices/{id}` |
| Team Add Member with Income Tax / GST / TDS / Accounting | UI `/team` |
| Master Departments CRUD | UI `/settings/master` |
| Session timer + one active session | Shell strip + login force-logout |
| Sync credits + Usage & Limits quotas | Shell credits chip + `/settings/usage` |

---

## Smoke checklist (manual UI)

Run API + web (host or Docker), then:

1. **Health** — `GET /health` → 200  
2. **Login** — `admin@noticesaas.local` / `Admin@12345`  
3. **Session** — timer visible; second login elsewhere can force-logout  
4. **Dashboard** — summary cards + task buckets load  
5. **Team** — list members; add member (Income Tax dept)  
6. **Master** — create/rename/delete a department  
7. **Clients** — add client with normal portal password; Sync now succeeds  
8. **OTP** — client with portal password `vault-otp`; Sync → enter OTP `123456`  
9. **Portal fail** — password `portal-timeout` fails with safe message (no secrets)  
10. **Notices** — tabs include Manual / Case Status; Add manual notice  
11. **Notice detail** — status, comment, reminder, assign, upload PDF/reply, download  
12. **Usage** — assessee seats + sync credit meters; credits decrement after sync  

### Mock portal passwords

| Password | Expected |
|----------|----------|
| (any normal) | Unattended sync |
| `vault-otp` | AwaitingOtp → OTP `123456` |
| `transient-once` | Retry then succeed |
| `portal-timeout` | Failed (safe message) |
| `wrong-password` | Rejected at add-client |

---

## Docker / packaging sanity

```powershell
# SQL only
docker compose up -d

# Full stack (API :8080, web :8088)
docker compose -f docker-compose.yml -f docker-compose.app.yml up -d --build
```

| Check | Expected |
|-------|----------|
| `http://localhost:8080/health` | 200 |
| `http://localhost:8088` | Angular shell / login |
| API logs | Migrations applied; no Key Vault errors when `KeyVault__Enabled=false` |
| Attachments | Local path `/data/notice-attachments` in container |

### Host run (dev)

```powershell
docker compose up -d
cd src/NoticeSaaS.Api
dotnet run
```

```powershell
cd web/notice-saas-web
npm start
```

Open http://localhost:4200

### Automated API smoke

With the API listening (default `http://localhost:5166` or set `$env:NOTICE_API_BASE`):

```powershell
./scripts/smoke-phase1.ps1
```

---

## Automated tests

```powershell
dotnet test tests/NoticeSaaS.UnitTests/NoticeSaaS.UnitTests.csproj
```

---

## Next

Phase 1.5 optional: AI notice analyzer, or treat Phase 1 as shippable.
