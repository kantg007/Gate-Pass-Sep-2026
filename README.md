# GateFlow

Boom-barrier access — **multi-tenant** (Park+ style):

| Who | Sees |
|-----|------|
| **Platform admin** (you) | All clients, suspend/activate |
| **Client** (society / RWA) | Only their sites, vehicles, visitors |
| **Guard** | Their site gate tools |
| **IoT device** | `POST /v1/access/check` with device key (no login) |

## Stack

| Folder | Tech |
|--------|------|
| `backend/` | .NET 8 solution (layered) |
| `frontend/` | React (Vite) |

### Backend solution layout

```
backend/
  GateFlow.sln
  docs/
  src/
    GateFlow.Api/             # Controllers, Program, Swagger, DI host
    GateFlow.Contracts/       # Request/response DTOs shared with UI clients
    GateFlow.Application/     # Abstractions (IAuthService, IAccessService), CurrentUser
    GateFlow.Domain/          # Entities + domain constants / SiteSettings
    GateFlow.Infrastructure/  # EF Core, migrations, seed, service implementations
```

## Database

- **Local:** SQLite (`gateflow.db`)
- **Production:** set `Database:Provider` = `SqlServer`

Full PARK+ style schema: [`backend/docs/db-design.md`](backend/docs/db-design.md).

## Run

```bash
cd backend && dotnet run --project src/GateFlow.Api
cd frontend && npm install && npm run dev
```

`dotnet run` opens **Swagger** (`launchUrl: swagger`). Root `/` redirects to Swagger.

- UI: http://127.0.0.1:5173  
- Health: http://127.0.0.1:8787/health (alias: `/api/health`)  
- **Swagger UI:** http://127.0.0.1:8787/swagger  

Open Swagger → `POST /v1/auth/login` → **Authorize** with the JWT → try Sites / Reports.  
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
