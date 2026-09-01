# Backend

.NET 8 layered solution.

| Project | Responsibility |
|---------|----------------|
| **GateFlow.Api** | HTTP host, Controllers, Swagger, auth middleware |
| **GateFlow.Contracts** | API request/response DTOs for UI / clients |
| **GateFlow.Application** | Abstractions (`IAuthService`, `IAccessService`), `CurrentUser` |
| **GateFlow.Domain** | Entities, roles, site settings |
| **GateFlow.Infrastructure** | EF Core, migrations, seed, handlers (`AuthHandler`, `AccessCheckHandler`) |

```bash
dotnet run --project src/GateFlow.Api
```

Swagger: http://127.0.0.1:8787/swagger
