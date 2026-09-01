namespace GateFlow.Api.Models;

/// <summary>Platform roles — Park+ style tenancy.</summary>
public static class Roles
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string ClientAdmin = "ClientAdmin";
    public const string Guard = "Guard";
}

/// <summary>Paying customer (RWA / facility company) — data isolated per client.</summary>
public class Client
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "Active"; // Active | Suspended | Pending
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<Site> Sites { get; set; } = new List<Site>();
}

public class AppUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary>PlatformAdmin | ClientAdmin | Guard</summary>
    public string Role { get; set; } = Roles.ClientAdmin;
    /// <summary>Null only for PlatformAdmin.</summary>
    public string? ClientId { get; set; }
    /// <summary>Optional: Guard scoped to one site.</summary>
    public string? SiteId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Client? Client { get; set; }
    public Site? Site { get; set; }
}

public class Site
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    /// <summary>JSON settings — change rules without redeploy.</summary>
    public string Settings { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Client Client { get; set; } = null!;
    public ICollection<Lane> Lanes { get; set; } = new List<Lane>();
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<AccessCredential> Credentials { get; set; } = new List<AccessCredential>();
    public ICollection<VisitorPass> VisitorPasses { get; set; } = new List<VisitorPass>();
    public ICollection<AccessEvent> Events { get; set; } = new List<AccessEvent>();
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}

public class Lane
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>ENTRY | EXIT | BOTH</summary>
    public string Direction { get; set; } = "ENTRY";
    public string DeviceApiKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Config { get; set; } = "{}";

    public Site Site { get; set; } = null!;
    public ICollection<AccessEvent> Events { get; set; } = new List<AccessEvent>();
}

public class Unit
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Block { get; set; }
    public string? Floor { get; set; }
    public string Meta { get; set; } = "{}";

    public Site Site { get; set; } = null!;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<VisitorPass> VisitorPasses { get; set; } = new List<VisitorPass>();
}

public class Vehicle
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    public string? UnitId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsActive { get; set; } = true;
    public string Meta { get; set; } = "{}";

    public Site Site { get; set; } = null!;
    public Unit? Unit { get; set; }
    public ICollection<AccessCredential> Credentials { get; set; } = new List<AccessCredential>();
}

public class AccessCredential
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    /// <summary>RFID | QR | BARCODE | ...</summary>
    public string Type { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? VehicleId { get; set; }
    public string? VisitorPassId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Site Site { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
    public VisitorPass? VisitorPass { get; set; }
}

public class VisitorPass
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    public string? UnitId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public int MaxUses { get; set; } = 2;
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Site Site { get; set; } = null!;
    public Unit? Unit { get; set; }
    public AccessCredential? Credential { get; set; }
}

public class AccessEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    public string? LaneId { get; set; }
    public string? CredentialType { get; set; }
    public string? CredentialCode { get; set; }
    /// <summary>ALLOW | DENY</summary>
    public string Decision { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? VehicleId { get; set; }
    public string? PlateNumber { get; set; }
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Site Site { get; set; } = null!;
    public Lane? Lane { get; set; }
}
