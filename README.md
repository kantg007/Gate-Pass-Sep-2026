# GateFlow

Society / office **boom-barrier access** — split stack:

| Folder | Role |
|--------|------|
| `backend/` | API (Hono + Prisma + SQLite) — IoT + UI call this |
| `frontend/` | React (Vite) admin / guard / mock gate |

Credential types are data-driven (`RFID`, `QR`, `BARCODE`, …). Site rules live in JSON `settings` so you can change limits/flags without rewriting code.

## Quick start

### 1) Backend (port **8787**)

```bash
cd backend
npm install
npx prisma db push
npm run db:seed
npm run dev
```

Health: http://127.0.0.1:8787/health

### 2) Frontend (port **5173**)

```bash
cd frontend
npm install
npm run dev
```

UI: http://127.0.0.1:5173

Demo seed:

- RFID: `RFID-1001` → ALLOW (MH12AB1234)
- Barcode: `BC-7788` → ALLOW
- Device key: `dev_demo_lane_key_001`
- Create visitor QR from a site page, then test on **Guard** / **Mock Gate**

## Core API

```http
POST /v1/access/check
X-Device-Key: dev_demo_lane_key_001
Content-Type: application/json

{ "credentialType": "RFID", "code": "RFID-1001" }
```

Or with `siteId` from the admin UI (no device key).

## Project layout

```
backend/src/index.ts          # HTTP routes
backend/src/services/access.ts # allow/deny rules
backend/prisma/schema.prisma  # dynamic multi-tenant models
frontend/src/pages/           # Sites, Guard, Mock Gate
frontend/src/lib/api.ts       # UI → API client
```

## Next (when you have ~30 min/day)

1. Auth for admin
2. Real QR image rendering
3. IoT box: read tag → call `/v1/access/check` → relay OPEN
4. Offline tag cache on controller

OTP / CPaaS is out of scope for this repo right now.
