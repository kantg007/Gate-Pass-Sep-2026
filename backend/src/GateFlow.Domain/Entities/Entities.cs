namespace GateFlow.Domain.Entities;

/// <summary>Built-in JWT / system roles (always present).</summary>
public static class Roles
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string ClientAdmin = "ClientAdmin";
    public const string Guard = "Guard";
    public const string SiteManager = "SiteManager";
    public const string Viewer = "Viewer";
}

/// <summary>Permission catalog keys used by RBAC.</summary>
public static class PermissionKeys
{
    public const string ClientManage = "client.manage";
    public const string SiteManage = "site.manage";
    public const string UserManage = "user.manage";
    public const string RoleManage = "role.manage";
    public const string GateManage = "gate.manage";
    public const string HardwareManage = "hardware.manage";
    public const string VehicleManage = "vehicle.manage";
    public const string VisitorManage = "visitor.manage";
    public const string GateManualOpen = "gate.manual_open";
    public const string GateManualClose = "gate.manual_close";
    public const string GateRemoteOpen = "gate.remote_open";
    public const string ReportView = "report.view";
    public const string AuditView = "audit.view";
    public const string SubscriptionView = "subscription.view";
}

/// <summary>Access event result types for reports: Pass / Fail / Manual open-close.</summary>
public static class AccessEventTypes
{
    public const string Pass = "PASS";
    public const string Fail = "FAIL";
    public const string ManualOpen = "MANUAL_OPEN";
    public const string ManualClose = "MANUAL_CLOSE";
}

public static class OpenMethods
{
    public const string Auto = "AUTO";
    public const string Guard = "GUARD";
    public const string Remote = "REMOTE";
    public const string System = "SYSTEM";
}

public static class HardwareStatuses
{
    public const string Online = "ONLINE";
    public const string Offline = "OFFLINE";
    public const string Degraded = "DEGRADED";
    public const string Unregistered = "UNREGISTERED";
}

public static class BarrierStates
{
    public const string Open = "OPEN";
    public const string Closed = "CLOSED";
    public const string Unknown = "UNKNOWN";
}

public static class AlertSeverities
{
    public const string Info = "INFO";
    public const string Warning = "WARNING";
    public const string Critical = "CRITICAL";
}

public static class AlertStatuses
{
    public const string Open = "OPEN";
    public const string Acknowledged = "ACKNOWLEDGED";
    public const string Resolved = "RESOLVED";
}

public static class AlertTypes
{
    public const string DeviceOffline = "DEVICE_OFFLINE";
    public const string AccessDenied = "ACCESS_DENIED";
    public const string BlacklistHit = "BLACKLIST_HIT";
    public const string Subscription = "SUBSCRIPTION";
    public const string GateOffline = "GATE_OFFLINE";
    public const string Manual = "MANUAL";
}

// ─── Platform / subscription ───────────────────────────────────────────────

/// <summary>Paying customer (society / mall / RWA) — tenant root.</summary>
public class Client
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
    /// <summary>Active | Suspended | Pending | Trial</summary>
    public string Status { get; set; } = "Active";
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<Site> Sites { get; set; } = new List<Site>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<HardwareDevice> Devices { get; set; } = new List<HardwareDevice>();
}

public class SubscriptionPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty; // STARTER | GROWTH | ENTERPRISE
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxSites { get; set; } = 1;
    public int MaxGates { get; set; } = 2;
    public int MaxVehicles { get; set; } = 200;
    public int MaxUsers { get; set; } = 10;
    public bool AllowAnpr { get; set; }
    public bool AllowVisitorModule { get; set; } = true;
    public bool AllowRemoteOpen { get; set; } = true;
    public bool AllowAntiPassback { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string Meta { get; set; } = "{}";

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}

public class Subscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClientId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    /// <summary>Trial | Active | Grace | Expired | Cancelled</summary>
    public string Status { get; set; } = "Active";
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;
    public DateTime EndsAt { get; set; }
    public DateTime? GraceEndsAt { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Client Client { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}

// ─── Users + RBAC ──────────────────────────────────────────────────────────

public class AppUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary>Primary system role for JWT: PlatformAdmin | ClientAdmin | SiteManager | Guard | Viewer</summary>
    public string Role { get; set; } = Roles.ClientAdmin;
    /// <summary>Null only for PlatformAdmin.</summary>
    public string? ClientId { get; set; }
    /// <summary>Optional default site scope (guards / site managers).</summary>
    public string? SiteId { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Client? Client { get; set; }
    public Site? Site { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserGateAssignment> GateAssignments { get; set; } = new List<UserGateAssignment>();
}

/// <summary>Global permission dictionary (seeded).</summary>
public class Permission
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty; // users | gates | vehicles | reports ...

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>Client-scoped custom role (or platform template when ClientId is null).</summary>
public class Role
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Client? Client { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public string PermissionId { get; set; } = string.Empty;

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

public class UserRole
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string? SiteId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedByUserId { get; set; }

    public AppUser User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public Site? Site { get; set; }
}

/// <summary>Guard / operator scoped to specific gates.</summary>
public class UserGateAssignment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string GateId { get; set; } = string.Empty;
    public bool CanManualOpen { get; set; } = true;
    public bool CanManualClose { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
    public Lane Gate { get; set; } = null!;
}

// ─── Sites / gates / units ─────────────────────────────────────────────────

public class Site
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Timezone { get; set; } = "Asia/Kolkata";
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
    public ICollection<HardwareDevice> Devices { get; set; } = new List<HardwareDevice>();
}

/// <summary>
/// Physical gate / boom lane (ENTRY | EXIT | BOTH).
/// Table name remains Lanes for compatibility; treat as Gate in product language.
/// </summary>
public class Lane
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    /// <summary>Denormalized for fast tenant filters / reports.</summary>
    public string? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // GATE-IN-1
    /// <summary>ENTRY | EXIT | BOTH</summary>
    public string Direction { get; set; } = "ENTRY";
    /// <summary>OPEN | CLOSED | UNKNOWN — physical boom position.</summary>
    public string BarrierState { get; set; } = BarrierStates.Closed;
    public DateTime? BarrierStateAt { get; set; }
    /// <summary>Legacy device key (prefer HardwareDevices.DeviceApiKey).</summary>
    public string DeviceApiKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string Config { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Site Site { get; set; } = null!;
    public Client? Client { get; set; }
    public ICollection<AccessEvent> Events { get; set; } = new List<AccessEvent>();
    public ICollection<HardwareDevice> Devices { get; set; } = new List<HardwareDevice>();
    public ICollection<UserGateAssignment> UserAssignments { get; set; } = new List<UserGateAssignment>();
    public ICollection<GateCommand> Commands { get; set; } = new List<GateCommand>();
    public ICollection<ManualOverride> ManualOverrides { get; set; } = new List<ManualOverride>();
}

public class Unit
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Block { get; set; }
    public string? Floor { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string Meta { get; set; } = "{}";
    public bool IsActive { get; set; } = true;

    public Site Site { get; set; } = null!;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<VisitorPass> VisitorPasses { get; set; } = new List<VisitorPass>();
}

// ─── Vehicles / credentials / visitors ─────────────────────────────────────

public class Vehicle
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? UnitId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? VehicleType { get; set; } // CAR | BIKE | SUV | OTHER
    public string? Color { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Site Site { get; set; } = null!;
    public Client? Client { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<AccessCredential> Credentials { get; set; } = new List<AccessCredential>();
}

public class AccessCredential
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    /// <summary>RFID | QR | BARCODE | ANPR | PIN | ...</summary>
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
    public string? GuestPhone { get; set; }
    public string? VehiclePlate { get; set; }
    public string? Purpose { get; set; }
    public int MaxUses { get; set; } = 2;
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Site Site { get; set; } = null!;
    public Unit? Unit { get; set; }
    public AccessCredential? Credential { get; set; }
}

// ─── Hardware ──────────────────────────────────────────────────────────────

public class HardwareDevice
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClientId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string? GateId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>CONTROLLER | RFID_READER | QR_READER | ANPR_CAM | BARRIER | RELAY</summary>
    public string DeviceType { get; set; } = "CONTROLLER";
    public string? SerialNumber { get; set; }
    public string? MacAddress { get; set; }
    public string DeviceApiKey { get; set; } = string.Empty;
    public string? FirmwareVersion { get; set; }
    /// <summary>ONLINE | OFFLINE | DEGRADED | UNREGISTERED</summary>
    public string ConnectionStatus { get; set; } = HardwareStatuses.Offline;
    public DateTime? LastSeenAt { get; set; }
    public DateTime? RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string Meta { get; set; } = "{}";

    public Client Client { get; set; } = null!;
    public Site Site { get; set; } = null!;
    public Lane? Gate { get; set; }
    public ICollection<DeviceHeartbeat> Heartbeats { get; set; } = new List<DeviceHeartbeat>();
}

public class DeviceHeartbeat
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Status { get; set; } = HardwareStatuses.Online;
    public string? FirmwareVersion { get; set; }
    public string? IpAddress { get; set; }
    public int? SignalRssi { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public HardwareDevice Device { get; set; } = null!;
}

// ─── Access events / commands / manual / audit ─────────────────────────────

public class AccessEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SiteId { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? LaneId { get; set; }
    public string? DeviceId { get; set; }
    public string? CredentialType { get; set; }
    public string? CredentialCode { get; set; }
    /// <summary>ALLOW | DENY (legacy decision)</summary>
    public string Decision { get; set; } = string.Empty;
    /// <summary>PASS | FAIL | MANUAL_OPEN | MANUAL_CLOSE — report-friendly</summary>
    public string EventType { get; set; } = AccessEventTypes.Fail;
    /// <summary>AUTO | GUARD | REMOTE | SYSTEM</summary>
    public string OpenMethod { get; set; } = OpenMethods.Auto;
    public string Reason { get; set; } = string.Empty;
    public string? VehicleId { get; set; }
    public string? PlateNumber { get; set; }
    public string? ActorUserId { get; set; }
    public string Meta { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Site Site { get; set; } = null!;
    public Client? Client { get; set; }
    public Lane? Lane { get; set; }
    public HardwareDevice? Device { get; set; }
    public AppUser? ActorUser { get; set; }
}

/// <summary>Command sent to hardware to open/close boom.</summary>
public class GateCommand
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string GateId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? DeviceId { get; set; }
    public string? AccessEventId { get; set; }
    /// <summary>OPEN | CLOSE | STOP | REBOOT</summary>
    public string Command { get; set; } = "OPEN";
    /// <summary>PENDING | SENT | ACKED | FAILED | TIMEOUT</summary>
    public string Status { get; set; } = "PENDING";
    public string? RequestedByUserId { get; set; }
    public string Source { get; set; } = OpenMethods.Auto;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? AckedAt { get; set; }

    public Lane Gate { get; set; } = null!;
    public Site Site { get; set; } = null!;
    public HardwareDevice? Device { get; set; }
    public AppUser? RequestedByUser { get; set; }
}

/// <summary>Guard / remote manual open-close with mandatory reason.</summary>
public class ManualOverride
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string GateId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    /// <summary>OPEN | CLOSE</summary>
    public string Action { get; set; } = "OPEN";
    /// <summary>GUARD | REMOTE</summary>
    public string Method { get; set; } = OpenMethods.Guard;
    /// <summary>VISITOR | AMBULANCE | TAG_FAIL | VIP | STUCK | OTHER</summary>
    public string ReasonCode { get; set; } = string.Empty;
    public string? ReasonNote { get; set; }
    public string? AccessEventId { get; set; }
    public string? GateCommandId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lane Gate { get; set; } = null!;
    public Site Site { get; set; } = null!;
    public AppUser ActorUser { get; set; } = null!;
}

/// <summary>Operational alert for dashboard / notifications.</summary>
public class Alert
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? ClientId { get; set; }
    public string? SiteId { get; set; }
    public string? GateId { get; set; }
    public string? DeviceId { get; set; }
    /// <summary>INFO | WARNING | CRITICAL</summary>
    public string Severity { get; set; } = AlertSeverities.Warning;
    /// <summary>DEVICE_OFFLINE | ACCESS_DENIED | BLACKLIST_HIT | …</summary>
    public string Type { get; set; } = AlertTypes.Manual;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>OPEN | ACKNOWLEDGED | RESOLVED</summary>
    public string Status { get; set; } = AlertStatuses.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string Meta { get; set; } = "{}";

    public Client? Client { get; set; }
    public Site? Site { get; set; }
    public Lane? Gate { get; set; }
    public HardwareDevice? Device { get; set; }
    public AppUser? AcknowledgedByUser { get; set; }
}

/// <summary>Immutable-ish audit trail for admin actions.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public string? ClientId { get; set; }
    public string? SiteId { get; set; }
    public string? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty; // USER_CREATE | VEHICLE_UPDATE | ROLE_ASSIGN ...
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Summary { get; set; }
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Client? Client { get; set; }
    public Site? Site { get; set; }
    public AppUser? ActorUser { get; set; }
}
