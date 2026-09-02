using System.Text.Json;
using GateFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GateFlow.Infrastructure.Persistence;

public class GateFlowDbContext : DbContext
{
    public GateFlowDbContext(DbContextOptions<GateFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserGateAssignment> UserGateAssignments => Set<UserGateAssignment>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Lane> Lanes => Set<Lane>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<AccessCredential> AccessCredentials => Set<AccessCredential>();
    public DbSet<VisitorPass> VisitorPasses => Set<VisitorPass>();
    public DbSet<HardwareDevice> HardwareDevices => Set<HardwareDevice>();
    public DbSet<DeviceHeartbeat> DeviceHeartbeats => Set<DeviceHeartbeat>();
    public DbSet<AccessEvent> AccessEvents => Set<AccessEvent>();
    public DbSet<GateCommand> GateCommands => Set<GateCommand>();
    public DbSet<ManualOverride> ManualOverrides => Set<ManualOverride>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(e =>
        {
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<SubscriptionPlan>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasIndex(x => new { x.ClientId, x.Status });
            e.HasIndex(x => x.EndsAt);
            e.HasOne(x => x.Client).WithMany(x => x.Subscriptions).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Plan).WithMany(x => x.Subscriptions).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => new { x.ClientId, x.Role });
            e.HasOne(x => x.Client).WithMany(x => x.Users).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Site).WithMany(x => x.Users).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(x => new { x.ClientId, x.Code }).IsUnique();
            e.HasOne(x => x.Client).WithMany(x => x.Roles).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(x => new { x.RoleId, x.PermissionId });
            e.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.RoleId, x.SiteId }).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserGateAssignment>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.GateId }).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.GateAssignments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Gate).WithMany(x => x.UserAssignments).HasForeignKey(x => x.GateId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Site>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.ClientId);
            e.HasOne(x => x.Client).WithMany(x => x.Sites).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lane>(e =>
        {
            e.ToTable("Lanes"); // product name = Gate
            e.HasIndex(x => x.DeviceApiKey).IsUnique();
            e.HasIndex(x => new { x.SiteId, x.Code });
            e.HasIndex(x => x.ClientId);
            e.HasOne(x => x.Site).WithMany(x => x.Lanes).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Unit>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.Label }).IsUnique();
            e.HasOne(x => x.Site).WithMany(x => x.Units).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.PlateNumber }).IsUnique();
            e.HasIndex(x => x.ClientId);
            e.HasIndex(x => new { x.IsBlacklisted, x.IsActive });
            e.HasOne(x => x.Site).WithMany(x => x.Vehicles).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.SetNull);
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
            e.HasIndex(x => new { x.SiteId, x.ValidUntil });
            e.HasOne(x => x.Site).WithMany(x => x.VisitorPasses).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Unit).WithMany(x => x.VisitorPasses).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HardwareDevice>(e =>
        {
            e.HasIndex(x => x.DeviceApiKey).IsUnique();
            e.HasIndex(x => x.SerialNumber);
            e.HasIndex(x => new { x.ClientId, x.SiteId });
            e.HasIndex(x => x.ConnectionStatus);
            e.HasOne(x => x.Client).WithMany(x => x.Devices).HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Site).WithMany(x => x.Devices).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Gate).WithMany(x => x.Devices).HasForeignKey(x => x.GateId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DeviceHeartbeat>(e =>
        {
            e.HasIndex(x => new { x.DeviceId, x.ReceivedAt });
            e.HasOne(x => x.Device).WithMany(x => x.Heartbeats).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccessEvent>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.CreatedAt });
            e.HasIndex(x => new { x.ClientId, x.CreatedAt });
            e.HasIndex(x => new { x.LaneId, x.EventType, x.CreatedAt });
            e.HasIndex(x => x.EventType);
            e.HasOne(x => x.Site).WithMany(x => x.Events).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Lane).WithMany(x => x.Events).HasForeignKey(x => x.LaneId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GateCommand>(e =>
        {
            e.HasIndex(x => new { x.GateId, x.CreatedAt });
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Gate).WithMany(x => x.Commands).HasForeignKey(x => x.GateId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ManualOverride>(e =>
        {
            e.HasIndex(x => new { x.SiteId, x.CreatedAt });
            e.HasIndex(x => new { x.GateId, x.CreatedAt });
            e.HasOne(x => x.Gate).WithMany(x => x.ManualOverrides).HasForeignKey(x => x.GateId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => new { x.ClientId, x.CreatedAt });
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.HasIndex(x => new { x.ClientId, x.Status, x.CreatedAt });
            e.HasIndex(x => new { x.SiteId, x.Status });
            e.HasIndex(x => x.Type);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Gate).WithMany().HasForeignKey(x => x.GateId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AcknowledgedByUser).WithMany().HasForeignKey(x => x.AcknowledgedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}

