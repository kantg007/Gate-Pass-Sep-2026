using System.Text.Json;
using GateFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Api.Data;

public class GateFlowDbContext : DbContext
{
    public GateFlowDbContext(DbContextOptions<GateFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Lane> Lanes => Set<Lane>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<AccessCredential> AccessCredentials => Set<AccessCredential>();
    public DbSet<VisitorPass> VisitorPasses => Set<VisitorPass>();
    public DbSet<AccessEvent> AccessEvents => Set<AccessEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Lane>(e =>
        {
            e.HasIndex(x => x.DeviceApiKey).IsUnique();
            e.HasOne(x => x.Site).WithMany(x => x.Lanes).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Unit>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.Label }).IsUnique();
            e.HasOne(x => x.Site).WithMany(x => x.Units).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.PlateNumber }).IsUnique();
            e.HasOne(x => x.Site).WithMany(x => x.Vehicles).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Unit).WithMany(x => x.Vehicles).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessCredential>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.Type, x.Code }).IsUnique();
            e.HasIndex(x => new { x.SiteId, x.Code });
            e.HasOne(x => x.Site).WithMany(x => x.Credentials).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vehicle).WithMany(x => x.Credentials).HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.VisitorPass).WithOne(x => x.Credential).HasForeignKey<AccessCredential>(x => x.VisitorPassId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VisitorPass>(e =>
        {
            e.HasOne(x => x.Site).WithMany(x => x.VisitorPasses).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Unit).WithMany(x => x.VisitorPasses).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AccessEvent>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.CreatedAt });
            e.HasOne(x => x.Site).WithMany(x => x.Events).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Lane).WithMany(x => x.Events).HasForeignKey(x => x.LaneId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}

public class SiteSettings
{
    public bool AllowManualOpen { get; set; } = true;
    public int VisitorDefaultMaxUses { get; set; } = 2;
    public int VisitorDefaultValidHours { get; set; } = 24;
    public bool RequireActiveVehicle { get; set; } = true;
    public bool DenyExpiredCredentials { get; set; } = true;
    public bool LogDeniedAttempts { get; set; } = true;
    public Dictionary<string, bool> Features { get; set; } = new()
    {
        ["rfid"] = true,
        ["qr"] = true,
        ["barcode"] = true,
        ["mockGate"] = true,
    };

    public static SiteSettings Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new SiteSettings();
        try
        {
            var parsed = JsonSerializer.Deserialize<SiteSettings>(raw, JsonOpts);
            if (parsed is null) return new SiteSettings();
            parsed.Features ??= new Dictionary<string, bool>();
            foreach (var (k, v) in new SiteSettings().Features)
            {
                parsed.Features.TryAdd(k, v);
            }
            return parsed;
        }
        catch
        {
            return new SiteSettings();
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}
