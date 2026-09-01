using GateFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeedAsync(GateFlowDbContext db)
    {
        await EnsurePlansAndPermissionsAsync(db);

        if (await db.Users.AnyAsync()) return;

        var starter = await db.SubscriptionPlans.FirstAsync(p => p.Code == "STARTER");

        var platformAdmin = new AppUser
        {
            Email = "admin@gateflow.local",
            FullName = "GateFlow Platform Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = Roles.PlatformAdmin,
            ClientId = null,
        };
        db.Users.Add(platformAdmin);

        var client = new Client
        {
            Name = "Green Valley Management",
            ContactEmail = "client@greenvalley.local",
            Phone = "9999999999",
            Status = "Active",
        };
        db.Clients.Add(client);

        db.Subscriptions.Add(new Subscription
        {
            ClientId = client.Id,
            PlanId = starter.Id,
            Status = "Active",
            StartsAt = DateTime.UtcNow.AddDays(-7),
            EndsAt = DateTime.UtcNow.AddMonths(1),
            GraceEndsAt = DateTime.UtcNow.AddMonths(1).AddDays(7),
        });

        var clientAdminRole = await EnsureClientDefaultRolesAsync(db, client.Id);

        var clientAdmin = new AppUser
        {
            Email = "client@greenvalley.local",
            FullName = "Society Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client@123"),
            Role = Roles.ClientAdmin,
            ClientId = client.Id,
        };
        db.Users.Add(clientAdmin);
        db.UserRoles.Add(new UserRole
        {
            UserId = clientAdmin.Id,
            RoleId = clientAdminRole.Id,
        });

        var settings = new SiteSettings
        {
            AllowManualOpen = true,
            AllowRemoteOpen = true,
            VisitorDefaultMaxUses = 2,
            Features =
            {
                ["rfid"] = true,
                ["qr"] = true,
                ["barcode"] = true,
                ["mockGate"] = true,
            },
        };

        var site = new Site
        {
            ClientId = client.Id,
            Name = "Green Valley Society",
            Slug = "green-valley",
            Address = "Pune, MH",
            Settings = settings.ToJson(),
        };
        db.Sites.Add(site);

        var guardRole = await db.Roles.FirstAsync(r => r.ClientId == client.Id && r.Code == "GUARD");
        var guard = new AppUser
        {
            Email = "guard@greenvalley.local",
            FullName = "Gate Guard",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Guard@123"),
            Role = Roles.Guard,
            ClientId = client.Id,
            SiteId = site.Id,
        };
        db.Users.Add(guard);
        db.UserRoles.Add(new UserRole
        {
            UserId = guard.Id,
            RoleId = guardRole.Id,
            SiteId = site.Id,
        });

        var entryGate = new Lane
        {
            SiteId = site.Id,
            ClientId = client.Id,
            Name = "Main Entry",
            Code = "GATE-IN-1",
            Direction = "ENTRY",
            DeviceApiKey = "dev_demo_lane_key_001",
        };
        var exitGate = new Lane
        {
            SiteId = site.Id,
            ClientId = client.Id,
            Name = "Main Exit",
            Code = "GATE-OUT-1",
            Direction = "EXIT",
            DeviceApiKey = "dev_demo_lane_key_002",
        };
        db.Lanes.AddRange(entryGate, exitGate);

        db.UserGateAssignments.Add(new UserGateAssignment
        {
            UserId = guard.Id,
            GateId = entryGate.Id,
            CanManualOpen = true,
            CanManualClose = true,
        });

        var controller = new HardwareDevice
        {
            ClientId = client.Id,
            SiteId = site.Id,
            GateId = entryGate.Id,
            Name = "Entry Controller-1",
            DeviceType = "CONTROLLER",
            SerialNumber = "GF-CTRL-0001",
            DeviceApiKey = "hw_demo_controller_001",
            ConnectionStatus = HardwareStatuses.Online,
            LastSeenAt = DateTime.UtcNow,
            FirmwareVersion = "1.0.0",
        };
        db.HardwareDevices.Add(controller);
        db.DeviceHeartbeats.Add(new DeviceHeartbeat
        {
            DeviceId = controller.Id,
            Status = HardwareStatuses.Online,
            FirmwareVersion = "1.0.0",
            IpAddress = "192.168.1.50",
            Payload = """{"uptimeSec":120}""",
        });

        var unitA = new Unit { SiteId = site.Id, Label = "A-101", Block = "A", Floor = "1", OwnerName = "Resident A" };
        var unitB = new Unit { SiteId = site.Id, Label = "B-204", Block = "B", Floor = "2", OwnerName = "Resident B" };
        db.Units.AddRange(unitA, unitB);

        var car1 = new Vehicle
        {
            SiteId = site.Id,
            ClientId = client.Id,
            UnitId = unitA.Id,
            PlateNumber = "MH12AB1234",
            Label = "Owner car",
            VehicleType = "CAR",
            OwnerName = "Resident A",
        };
        var car2 = new Vehicle
        {
            SiteId = site.Id,
            ClientId = client.Id,
            UnitId = unitB.Id,
            PlateNumber = "MH14CD5678",
            Label = "Second car",
            VehicleType = "CAR",
            OwnerName = "Resident B",
        };
        db.Vehicles.AddRange(car1, car2);

        db.AccessCredentials.AddRange(
            new AccessCredential
            {
                SiteId = site.Id,
                Type = "RFID",
                Code = "RFID-1001",
                VehicleId = car1.Id,
            },
            new AccessCredential
            {
                SiteId = site.Id,
                Type = "BARCODE",
                Code = "BC-7788",
                VehicleId = car2.Id,
            });

        var validUntil = DateTime.UtcNow.AddHours(24);
        var pass = new VisitorPass
        {
            SiteId = site.Id,
            UnitId = unitA.Id,
            GuestName = "Ravi Guest",
            Purpose = "Family visit",
            MaxUses = 2,
            ValidUntil = validUntil,
            CreatedByUserId = clientAdmin.Id,
        };
        db.VisitorPasses.Add(pass);

        db.AccessCredentials.Add(new AccessCredential
        {
            SiteId = site.Id,
            Type = "QR",
            Code = "VIS-DEMO-001",
            VisitorPassId = pass.Id,
            ExpiresAt = validUntil,
        });

        db.AuditLogs.Add(new AuditLog
        {
            ClientId = client.Id,
            SiteId = site.Id,
            ActorUserId = platformAdmin.Id,
            Action = "SEED_DEMO",
            EntityType = "Client",
            EntityId = client.Id,
            Summary = "Demo tenant seeded with site, gates, hardware, vehicles",
        });

        await db.SaveChangesAsync();
    }

    private static async Task EnsurePlansAndPermissionsAsync(GateFlowDbContext db)
    {
        if (!await db.SubscriptionPlans.AnyAsync())
        {
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    Code = "STARTER",
                    Name = "Starter",
                    Description = "1 site, 2 gates, 200 vehicles — societies getting started",
                    MaxSites = 1,
                    MaxGates = 2,
                    MaxVehicles = 200,
                    MaxUsers = 10,
                    AllowVisitorModule = true,
                    AllowRemoteOpen = true,
                    MonthlyPrice = 1999,
                },
                new SubscriptionPlan
                {
                    Code = "GROWTH",
                    Name = "Growth",
                    Description = "3 sites, 8 gates, 1000 vehicles — multi-tower societies / small malls",
                    MaxSites = 3,
                    MaxGates = 8,
                    MaxVehicles = 1000,
                    MaxUsers = 40,
                    AllowAnpr = true,
                    AllowVisitorModule = true,
                    AllowRemoteOpen = true,
                    AllowAntiPassback = true,
                    MonthlyPrice = 5999,
                },
                new SubscriptionPlan
                {
                    Code = "ENTERPRISE",
                    Name = "Enterprise",
                    Description = "Unlimited-ish ops for large malls / campuses",
                    MaxSites = 50,
                    MaxGates = 100,
                    MaxVehicles = 20000,
                    MaxUsers = 500,
                    AllowAnpr = true,
                    AllowVisitorModule = true,
                    AllowRemoteOpen = true,
                    AllowAntiPassback = true,
                    MonthlyPrice = 24999,
                });
        }

        if (!await db.Permissions.AnyAsync())
        {
            var perms = new (string Key, string Name, string Module)[]
            {
                (PermissionKeys.ClientManage, "Manage client profile", "client"),
                (PermissionKeys.SiteManage, "Manage sites", "sites"),
                (PermissionKeys.UserManage, "Create/manage users", "users"),
                (PermissionKeys.RoleManage, "Create/assign roles", "roles"),
                (PermissionKeys.GateManage, "Manage gates", "gates"),
                (PermissionKeys.HardwareManage, "Register/manage hardware", "hardware"),
                (PermissionKeys.VehicleManage, "Manage vehicles", "vehicles"),
                (PermissionKeys.VisitorManage, "Manage visitor passes", "visitors"),
                (PermissionKeys.GateManualOpen, "Manual open gate", "gates"),
                (PermissionKeys.GateManualClose, "Manual close gate", "gates"),
                (PermissionKeys.GateRemoteOpen, "Remote open gate", "gates"),
                (PermissionKeys.ReportView, "View reports", "reports"),
                (PermissionKeys.AuditView, "View audit logs", "audit"),
                (PermissionKeys.SubscriptionView, "View subscription", "billing"),
            };
            foreach (var (key, name, module) in perms)
            {
                db.Permissions.Add(new Permission { Key = key, Name = name, Module = module });
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Creates default client roles with permission maps. Returns ClientAdmin role.</summary>
    public static async Task<Role> EnsureClientDefaultRolesAsync(GateFlowDbContext db, string clientId)
    {
        var existing = await db.Roles.FirstOrDefaultAsync(r => r.ClientId == clientId && r.Code == "CLIENT_ADMIN");
        if (existing is not null) return existing;

        var allPerms = await db.Permissions.ToListAsync();
        Permission P(string key) => allPerms.First(x => x.Key == key);

        var admin = new Role
        {
            ClientId = clientId,
            Name = "Client Admin",
            Code = "CLIENT_ADMIN",
            IsSystem = true,
            Description = "Full access within client tenant",
        };
        var manager = new Role
        {
            ClientId = clientId,
            Name = "Site Manager",
            Code = "SITE_MANAGER",
            IsSystem = true,
            Description = "Manage one or more sites",
        };
        var guard = new Role
        {
            ClientId = clientId,
            Name = "Guard",
            Code = "GUARD",
            IsSystem = true,
            Description = "Gate operations and manual open/close",
        };
        var viewer = new Role
        {
            ClientId = clientId,
            Name = "Viewer",
            Code = "VIEWER",
            IsSystem = true,
            Description = "Read-only reports",
        };
        db.Roles.AddRange(admin, manager, guard, viewer);

        void Map(Role role, params string[] keys)
        {
            foreach (var key in keys)
            {
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = P(key).Id });
            }
        }

        Map(admin, allPerms.Select(p => p.Key).ToArray());
        Map(manager,
            PermissionKeys.SiteManage, PermissionKeys.GateManage, PermissionKeys.HardwareManage,
            PermissionKeys.VehicleManage, PermissionKeys.VisitorManage, PermissionKeys.UserManage,
            PermissionKeys.GateManualOpen, PermissionKeys.GateManualClose, PermissionKeys.GateRemoteOpen,
            PermissionKeys.ReportView, PermissionKeys.AuditView);
        Map(guard,
            PermissionKeys.GateManualOpen, PermissionKeys.GateManualClose,
            PermissionKeys.VisitorManage, PermissionKeys.ReportView);
        Map(viewer, PermissionKeys.ReportView, PermissionKeys.SubscriptionView);

        await db.SaveChangesAsync();
        return admin;
    }
}
