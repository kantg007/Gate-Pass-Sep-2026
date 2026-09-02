# GateFlow / GatePass

Boom-barrier access — **multi-tenant** (Park+ style) with a GatePass ops dashboard UI.

| Who | Sees |
|-----|------|
| **Platform admin** (you) | Dashboard, all clients, suspend/activate |
| **Client** (society / RWA) | Dashboard, their sites, vehicles, visitors, gates, hardware, alerts |
| **Guard** | Dashboard + gate tools |
| **IoT device** | `POST /v1/access/check` with device key (no login) |

## Stack

| Folder | Tech |
|--------|------|
| `backend/` | .NET 8 solution (layered) |
| `frontend/` | React (Vite) + GatePass light ops theme |

### Key dashboard APIs

- `GET /v1/dashboard/overview?siteId=` — KPIs, 24h movement, gate status, live gates, activity, top sites, device health
- `GET /v1/gates` · `POST /v1/gates/{id}/commands` — status + remote open/close
- `GET /v1/hardware` · `POST /v1/hardware/heartbeat` — devices + heartbeat
- `GET /v1/alerts` · `PATCH /v1/alerts/{id}` — operational alerts
- `GET /v1/users` · `GET /v1/roles` · `GET /v1/search?q=`
- `GET /v1/reports/vehicle-movement` · `GET /v1/reports/top-sites`

## Database

- **Local:** SQLite (`gateflow.db`)
- **Production:** set `Database:Provider` = `SqlServer`

Full PARK+ style schema: [`backend/docs/db-design.md`](backend/docs/db-design.md).

## Run

```bash
cd backend && dotnet run --project src/GateFlow.Api
cd frontend && npm install && npm run dev
```

`dotnet run` opens **Swagger** in the browser (`launchUrl: swagger`). Root `/` also redirects to Swagger.

- UI: http://127.0.0.1:5173  
- Health: http://127.0.0.1:8787/health (alias: `/api/health`)  
- **Swagger UI:** http://127.0.0.1:8787/swagger  

Open Swagger → `POST /v1/auth/login` → **Authorize** with the JWT → try Sites / Reports / Dashboard.  
Device endpoint `POST /v1/access/check` uses header `X-Device-Key` (`dev_demo_lane_key_001`).

### Demo accounts

| Role | Email | Password |
|------|-------|----------|
| Platform admin | admin@gateflow.local | Admin@123 |
| Client admin | client@greenvalley.local | Client@123 |
| Guard | guard@greenvalley.local | Guard@123 |

Register a new client from `/register` — creates isolated `Client` + `ClientAdmin` user.

### Gate demos

- RFID `RFID-1001`, barcode `BC-7788`, visitor QR `VIS-DEMO-001`
- Device key `dev_demo_lane_key_001`
