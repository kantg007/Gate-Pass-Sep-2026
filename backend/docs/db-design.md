# GateFlow DB Design (PARK+ style)

Multi-tenant boom-barrier / parking access platform for societies, malls, campuses.

**Product language:** Gate = `Lanes` table (kept name for API compatibility).

## Hierarchy

```
Platform Admin
 └─ Client (tenant / society / mall)
     ├─ Subscription → SubscriptionPlan
     ├─ Roles + Permissions (RBAC)
     ├─ Users (+ UserRoles, UserGateAssignments)
     └─ Sites
         ├─ Gates (Lanes)
         │    ├─ HardwareDevices (+ DeviceHeartbeats)
         │    ├─ GateCommands
         │    └─ ManualOverrides
         ├─ Units (flats / shops)
         ├─ Vehicles (+ AccessCredentials)
         ├─ VisitorPasses (+ AccessCredentials)
         └─ AccessEvents  → reports: PASS / FAIL / MANUAL_OPEN / MANUAL_CLOSE
 AuditLogs (cross-cutting)
```

## Table map

| Table | Purpose |
|-------|---------|
| `Clients` | Paying tenant |
| `SubscriptionPlans` | STARTER / GROWTH / ENTERPRISE limits |
| `Subscriptions` | Active billing window + grace |
| `Users` | Login accounts; `Role` = JWT system role |
| `Permissions` | Permission catalog |
| `Roles` | Client-custom + system template roles |
| `RolePermissions` | Role ↔ permission |
| `UserRoles` | User ↔ role (optional site scope) |
| `UserGateAssignments` | Guard ↔ specific gates |
| `Sites` | Society tower / mall basement / campus block |
| `Lanes` | **Gates** (ENTRY / EXIT / BOTH) |
| `Units` | Flat / shop labels |
| `Vehicles` | Plates, blacklist, validity |
| `AccessCredentials` | RFID / QR / BARCODE / ANPR codes |
| `VisitorPasses` | Temporary guest access |
| `HardwareDevices` | Controllers / readers registration |
| `DeviceHeartbeats` | Online/offline evidence |
| `AccessEvents` | Every attempt + report event type |
| `GateCommands` | OPEN/CLOSE commands to hardware |
| `ManualOverrides` | Guard/remote open with reason codes |
| `AuditLogs` | Admin/config change trail |

## Report shape (Client → Site → Gate)

Filter `AccessEvents` by `ClientId`, `SiteId`, `LaneId`, date range, group by `EventType`:

- `PASS` — auto allow
- `FAIL` — deny (unknown, expired, blacklist, subscription, …)
- `MANUAL_OPEN` / `MANUAL_CLOSE` — guard or remote

API: `GET /v1/reports/access-summary`

## Negative-path fields (built into schema)

| Scenario | Where it lives |
|----------|----------------|
| Subscription expired / grace | `Subscriptions.Status`, `EndsAt`, `GraceEndsAt` |
| Client suspended | `Clients.Status` |
| Vehicle blacklist / expiry | `Vehicles.IsBlacklisted`, `ValidFrom/Until` |
| Credential expiry | `AccessCredentials.ExpiresAt` |
| Hardware offline | `HardwareDevices.ConnectionStatus`, `LastSeenAt` |
| Unauthorized gate for guard | `UserGateAssignments` missing row |
| Manual without reason | enforced in API using `ManualOverrides.ReasonCode` |
| Duplicate plate / tag | unique indexes on plate & credential |

## Indexes worth remembering

- `(SiteId, PlateNumber)` unique — vehicles
- `(SiteId, Type, Code)` unique — credentials
- `(SiteId, CreatedAt)`, `(LaneId, EventType, CreatedAt)` — events/reports
- `DeviceApiKey` unique — lanes + hardware
- `(ClientId, Code)` unique — roles

## Seed demo

| Role | Email | Password |
|------|-------|----------|
| Platform admin | admin@gateflow.local | Admin@123 |
| Client admin | client@greenvalley.local | Client@123 |
| Guard | guard@greenvalley.local | Guard@123 |

RFID `RFID-1001`, barcode `BC-7788`, visitor QR `VIS-DEMO-001`, lane key `dev_demo_lane_key_001`.
