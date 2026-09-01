using GateFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeedAsync(GateFlowDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

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

        var clientAdmin = new AppUser
        {
            Email = "client@greenvalley.local",
            FullName = "Society Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client@123"),
            Role = Roles.ClientAdmin,
            ClientId = client.Id,
        };
        db.Users.Add(clientAdmin);

        var settings = new SiteSettings
        {
            AllowManualOpen = true,
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
            Settings = settings.ToJson(),
        };
        db.Sites.Add(site);

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

        db.Lanes.Add(new Lane
        {
            SiteId = site.Id,
            Name = "Main Entry",
            Direction = "ENTRY",
            DeviceApiKey = "dev_demo_lane_key_001",
        });

        var unitA = new Unit { SiteId = site.Id, Label = "A-101", Block = "A", Floor = "1" };
        var unitB = new Unit { SiteId = site.Id, Label = "B-204", Block = "B", Floor = "2" };
        db.Units.AddRange(unitA, unitB);

        var car1 = new Vehicle
        {
            SiteId = site.Id,
            UnitId = unitA.Id,
            PlateNumber = "MH12AB1234",
            Label = "Owner car",
        };
        var car2 = new Vehicle
        {
            SiteId = site.Id,
            UnitId = unitB.Id,
            PlateNumber = "MH14CD5678",
            Label = "Second car",
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

        await db.SaveChangesAsync();
    }
}
