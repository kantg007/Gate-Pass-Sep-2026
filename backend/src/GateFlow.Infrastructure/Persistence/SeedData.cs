using GateFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Infrastructure.Persistence;

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
            Name = "Main Entry Gate",
            Code = "GATE-IN-1",
            Direction = "ENTRY",
            DeviceApiKey = "dev_demo_lane_key_001",
            BarrierState = BarrierStates.Open,
            BarrierStateAt = DateTime.UtcNow.AddMinutes(-5),
        };
        var exitGate = new Lane
        {
            SiteId = site.Id,
            ClientId = client.Id,
            Name = "Main Exit Gate",
            Code = "GATE-OUT-1",
            Direction = "EXIT",
            DeviceApiKey = "dev_demo_lane_key_002",
            BarrierState = BarrierStates.Closed,
            BarrierStateAt = DateTime.UtcNow.AddMinutes(-2),
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

        // Second site + richer demo telemetry for dashboard charts
        var site2 = new Site
        {
            ClientId = client.Id,
            Name = "Tech Park Bangalore",
            Slug = "tech-park-blr",
            Address = "Bangalore, KA",
            Settings = settings.ToJson(),
        };
        db.Sites.Add(site2);
        var site2Entry = new Lane
        {
            SiteId = site2.Id,
            ClientId = client.Id,
            Name = "Tower A Entry",
            Code = "TP-IN-1",
            Direction = "ENTRY",
            DeviceApiKey = "dev_demo_lane_key_003",
            BarrierState = BarrierStates.Closed,
            BarrierStateAt = DateTime.UtcNow,
        };
        var site2Exit = new Lane
        {
            SiteId = site2.Id,
            ClientId = client.Id,
            Name = "Tower A Exit",
            Code = "TP-OUT-1",
            Direction = "EXIT",
            DeviceApiKey = "dev_demo_lane_key_004",
            BarrierState = BarrierStates.Closed,
            BarrierStateAt = DateTime.UtcNow,
        };
        db.Lanes.AddRange(site2Entry, site2Exit);

        var reader = new HardwareDevice
        {
            ClientId = client.Id,
            SiteId = site.Id,
            GateId = exitGate.Id,
            Name = "Exit RFID Reader",
            DeviceType = "RFID_READER",
            SerialNumber = "GF-RFID-0002",
            DeviceApiKey = "hw_demo_reader_002",
            ConnectionStatus = HardwareStatuses.Online,
            LastSeenAt = DateTime.UtcNow.AddMinutes(-1),
            FirmwareVersion = "1.0.0",
        };
        var offlineCam = new HardwareDevice
        {
            ClientId = client.Id,
            SiteId = site2.Id,
            GateId = site2Entry.Id,
            Name = "ANPR Cam Tower A",
            DeviceType = "ANPR_CAM",
            SerialNumber = "GF-ANPR-0003",
            DeviceApiKey = "hw_demo_anpr_003",
            ConnectionStatus = HardwareStatuses.Offline,
            LastSeenAt = DateTime.UtcNow.AddMinutes(-45),
            FirmwareVersion = "0.9.1",
        };
        var warnRelay = new HardwareDevice
        {
            ClientId = client.Id,
            SiteId = site2.Id,
            GateId = site2Exit.Id,
            Name = "Exit Relay",
            DeviceType = "RELAY",
            SerialNumber = "GF-RLY-0004",
            DeviceApiKey = "hw_demo_relay_004",
            ConnectionStatus = HardwareStatuses.Degraded,
            LastSeenAt = DateTime.UtcNow.AddMinutes(-3),
            FirmwareVersion = "1.0.0",
        };
        db.HardwareDevices.AddRange(reader, offlineCam, warnRelay);

        db.Alerts.AddRange(
            new Alert
            {
                ClientId = client.Id,
                SiteId = site2.Id,
                GateId = site2Entry.Id,
                DeviceId = offlineCam.Id,
                Severity = AlertSeverities.Critical,
                Type = AlertTypes.DeviceOffline,
                Title = "Device offline",
                Message = "ANPR Cam Tower A has not sent a heartbeat.",
            },
            new Alert
            {
                ClientId = client.Id,
                SiteId = site.Id,
                Severity = AlertSeverities.Warning,
                Type = AlertTypes.AccessDenied,
                Title = "Access denied",
                Message = "Unknown credential attempted at Main Entry Gate.",
            },
            new Alert
            {
                ClientId = client.Id,
                SiteId = site2.Id,
                DeviceId = warnRelay.Id,
                Severity = AlertSeverities.Warning,
                Type = AlertTypes.Manual,
                Title = "Device degraded",
                Message = "Exit Relay reporting intermittent signal.",
            });

        // Synthetic 24h movement + recent activity
        var rnd = new Random(42);
        var plates = new[] { "KA01AB1234", "MH12AB1234", "MH14CD5678", "KA05XY9988", "DL01CA4455" };
        for (var h = 23; h >= 0; h--)
        {
            var hour = DateTime.UtcNow.AddHours(-h);
            hour = new DateTime(hour.Year, hour.Month, hour.Day, hour.Hour, 0, 0, DateTimeKind.Utc);
            var entries = 8 + rnd.Next(0, 25);
            var exits = 6 + rnd.Next(0, 20);
            for (var i = 0; i < entries; i++)
            {
                var gate = rnd.Next(0, 2) == 0 ? entryGate : site2Entry;
                db.AccessEvents.Add(new AccessEvent
                {
                    SiteId = gate.SiteId,
                    ClientId = client.Id,
                    LaneId = gate.Id,
                    Decision = "ALLOW",
                    EventType = AccessEventTypes.Pass,
                    OpenMethod = OpenMethods.Auto,
                    Reason = "CREDENTIAL_OK",
                    PlateNumber = plates[rnd.Next(plates.Length)],
                    CreatedAt = hour.AddMinutes(rnd.Next(0, 59)),
                });
            }
            for (var i = 0; i < exits; i++)
            {
                var gate = rnd.Next(0, 2) == 0 ? exitGate : site2Exit;
                db.AccessEvents.Add(new AccessEvent
                {
                    SiteId = gate.SiteId,
                    ClientId = client.Id,
                    LaneId = gate.Id,
                    Decision = "ALLOW",
                    EventType = AccessEventTypes.Pass,
                    OpenMethod = OpenMethods.Auto,
                    Reason = "CREDENTIAL_OK",
                    PlateNumber = plates[rnd.Next(plates.Length)],
                    CreatedAt = hour.AddMinutes(rnd.Next(0, 59)),
                });
            }
        }

        db.AccessEvents.AddRange(
            new AccessEvent
            {
                SiteId = site.Id,
                ClientId = client.Id,
                LaneId = entryGate.Id,
                Decision = "DENY",
                EventType = AccessEventTypes.Fail,
                OpenMethod = OpenMethods.Auto,
                Reason = "UNKNOWN_CREDENTIAL",
                PlateNumber = "KA99ZZ0001",
                CreatedAt = DateTime.UtcNow.AddMinutes(-4),
            },
            new AccessEvent
            {
                SiteId = site.Id,
                ClientId = client.Id,
                LaneId = entryGate.Id,
                Decision = "ALLOW",
                EventType = AccessEventTypes.ManualOpen,
                OpenMethod = OpenMethods.Guard,
                Reason = "VISITOR",
                ActorUserId = guard.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            },
            new AccessEvent
            {
                SiteId = site2.Id,
                ClientId = client.Id,
                LaneId = site2Entry.Id,
                Decision = "ALLOW",
                EventType = AccessEventTypes.Pass,
                OpenMethod = OpenMethods.Auto,
                Reason = "CREDENTIAL_OK",
                PlateNumber = "KA01AB1234",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            });

        // Extra demo client for platform "companies" KPI
        var client2 = new Client
        {
            Name = "Sunrise Mall Ops",
            ContactEmail = "ops@sunrisemall.local",
            Phone = "8888888888",
            Status = "Active",
        };
        db.Clients.Add(client2);
        db.Subscriptions.Add(new Subscription
        {
            ClientId = client2.Id,
            PlanId = starter.Id,
            Status = "Active",
            StartsAt = DateTime.UtcNow.AddDays(-30),
            EndsAt = DateTime.UtcNow.AddMonths(2),
        });
        await EnsureClientDefaultRolesAsync(db, client2.Id);
        var mallSite = new Site
        {
            ClientId = client2.Id,
            Name = "Sunrise Mall Basement",
            Slug = "sunrise-mall",
            Address = "Hyderabad, TS",
            Settings = settings.ToJson(),
        };
        db.Sites.Add(mallSite);
        db.Lanes.Add(new Lane
        {
            SiteId = mallSite.Id,
            ClientId = client2.Id,
            Name = "Basement Entry",
            Code = "SM-IN-1",
            Direction = "ENTRY",
            DeviceApiKey = "dev_demo_lane_key_005",
            BarrierState = BarrierStates.Open,
            BarrierStateAt = DateTime.UtcNow,
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
