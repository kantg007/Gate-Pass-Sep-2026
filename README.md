# GateFlow

Society / office **boom-barrier access** — split stack:

| Folder | Role |
|--------|------|
| `backend/` | **.NET 8** Web API + **EF Core Code First** |
| `frontend/` | React (Vite) — calls the API |

IoT controllers will also call the same API (`POST /v1/access/check`).

## Database (EF Core Code First)

Configured in `backend/appsettings.json` → `Database:Provider`:

| Provider | When |
|----------|------|
| **Sqlite** (default) | Local / MVP — zero install (`gateflow.db`) |
| **SqlServer** | Production / Windows hosting — change provider + connection string |

Models live in `backend/Models/`. Migrations in `backend/Migrations/`.

```bash
cd backend
dotnet ef migrations add YourChange
dotnet ef database update
```

On startup the API runs `Database.Migrate()` + demo seed (if empty).

## Quick start

### Backend (port **8787**)

```bash
cd backend
dotnet restore
dotnet run
```

Health: http://127.0.0.1:8787/health  
Swagger (dev): http://127.0.0.1:8787/swagger

### Frontend (port **5173**)

```bash
cd frontend
npm install
npm run dev
```

UI: http://127.0.0.1:5173

### Demo credentials (seed)

- RFID: `RFID-1001` → ALLOW (`MH12AB1234`)
- Barcode: `BC-7788` → ALLOW
- Visitor QR: `VIS-DEMO-001` → ALLOW
- Device key: `dev_demo_lane_key_001`

## Core API

```http
POST /v1/access/check
X-Device-Key: dev_demo_lane_key_001
Content-Type: application/json

{ "credentialType": "RFID", "code": "RFID-1001" }
```

## Switch to SQL Server

`appsettings.json`:

```json
"Database": {
  "Provider": "SqlServer",
  "ConnectionStrings": {
    "SqlServer": "Server=...;Database=GateFlow;..."
  }
}
```

Then:

```bash
dotnet ef database update
dotnet run
```

## Layout

```
backend/
  Models/Entities.cs
  Data/GateFlowDbContext.cs   # EF Core DbContext
  Data/SeedData.cs
  Services/AccessService.cs   # ALLOW / DENY rules
  Migrations/                 # Code First migrations
  Program.cs                  # Minimal APIs
frontend/
  src/pages/                  # Sites, Guard, Mock Gate
  src/lib/api.ts
```
