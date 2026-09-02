using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GateFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParkPlusDbDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitorPasses_SiteId",
                table: "VisitorPasses");

            migrationBuilder.DropIndex(
                name: "IX_Users_ClientId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Lanes_SiteId",
                table: "Lanes");

            migrationBuilder.DropIndex(
                name: "IX_AccessEvents_LaneId",
                table: "AccessEvents");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "VisitorPasses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "VisitorPasses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiclePlate",
                table: "VisitorPasses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlacklistReason",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Vehicles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsBlacklisted",
                table: "Vehicles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerPhone",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Vehicles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Units",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "Units",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerPhone",
                table: "Units",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "Sites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Lanes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Lanes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Lanes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Lanes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ActorUserId",
                table: "AccessEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "AccessEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "AccessEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "AccessEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OpenMethod",
                table: "AccessEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<string>(type: "TEXT", nullable: true),
                    SiteId = table.Column<string>(type: "TEXT", nullable: true),
                    ActorUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    BeforeJson = table.Column<string>(type: "TEXT", nullable: false),
                    AfterJson = table.Column<string>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HardwareDevices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", nullable: false),
                    GateId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    MacAddress = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    FirmwareVersion = table.Column<string>(type: "TEXT", nullable: true),
                    ConnectionStatus = table.Column<string>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Meta = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HardwareDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HardwareDevices_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HardwareDevices_Lanes_GateId",
                        column: x => x.GateId,
                        principalTable: "Lanes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HardwareDevices_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManualOverrides",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    GateId = table.Column<string>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: true),
                    ActorUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Method = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonCode = table.Column<string>(type: "TEXT", nullable: false),
                    ReasonNote = table.Column<string>(type: "TEXT", nullable: true),
                    AccessEventId = table.Column<string>(type: "TEXT", nullable: true),
                    GateCommandId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualOverrides_Lanes_GateId",
                        column: x => x.GateId,
                        principalTable: "Lanes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManualOverrides_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManualOverrides_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Module = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    MaxSites = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxGates = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxVehicles = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowAnpr = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowVisitorModule = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowRemoteOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowAntiPassback = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Meta = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGateAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    GateId = table.Column<string>(type: "TEXT", nullable: false),
                    CanManualOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManualClose = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGateAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGateAssignments_Lanes_GateId",
                        column: x => x.GateId,
                        principalTable: "Lanes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGateAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceHeartbeats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    FirmwareVersion = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    SignalRssi = table.Column<int>(type: "INTEGER", nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceHeartbeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceHeartbeats_HardwareDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "HardwareDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GateCommands",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    GateId = table.Column<string>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: true),
                    AccessEventId = table.Column<string>(type: "TEXT", nullable: true),
                    Command = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AckedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GateCommands_HardwareDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "HardwareDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GateCommands_Lanes_GateId",
                        column: x => x.GateId,
                        principalTable: "Lanes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GateCommands_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GateCommands_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    PermissionId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    SiteId = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssignedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    PlanId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GraceEndsAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AutoRenew = table.Column<bool>(type: "INTEGER", nullable: false),
                    Meta = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitorPasses_SiteId_ValidUntil",
                table: "VisitorPasses",
                columns: new[] { "SiteId", "ValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ClientId",
                table: "Vehicles",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_IsBlacklisted_IsActive",
                table: "Vehicles",
                columns: new[] { "IsBlacklisted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClientId_Role",
                table: "Users",
                columns: new[] { "ClientId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_ClientId",
                table: "Lanes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_SiteId_Code",
                table: "Lanes",
                columns: new[] { "SiteId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Status",
                table: "Clients",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_ActorUserId",
                table: "AccessEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_ClientId_CreatedAt",
                table: "AccessEvents",
                columns: new[] { "ClientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_DeviceId",
                table: "AccessEvents",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_EventType",
                table: "AccessEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_LaneId_EventType_CreatedAt",
                table: "AccessEvents",
                columns: new[] { "LaneId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClientId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "ClientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SiteId",
                table: "AuditLogs",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHeartbeats_DeviceId_ReceivedAt",
                table: "DeviceHeartbeats",
                columns: new[] { "DeviceId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GateCommands_DeviceId",
                table: "GateCommands",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_GateCommands_GateId_CreatedAt",
                table: "GateCommands",
                columns: new[] { "GateId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GateCommands_RequestedByUserId",
                table: "GateCommands",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GateCommands_SiteId",
                table: "GateCommands",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_GateCommands_Status",
                table: "GateCommands",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HardwareDevices_ClientId_SiteId",
                table: "HardwareDevices",
                columns: new[] { "ClientId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_HardwareDevices_ConnectionStatus",
                table: "HardwareDevices",
                column: "ConnectionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_HardwareDevices_DeviceApiKey",
                table: "HardwareDevices",
                column: "DeviceApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HardwareDevices_GateId",
                table: "HardwareDevices",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_HardwareDevices_SerialNumber",
                table: "HardwareDevices",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HardwareDevices_SiteId",
                table: "HardwareDevices",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualOverrides_ActorUserId",
                table: "ManualOverrides",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualOverrides_GateId_CreatedAt",
                table: "ManualOverrides",
                columns: new[] { "GateId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualOverrides_SiteId_CreatedAt",
                table: "ManualOverrides",
                columns: new[] { "SiteId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ClientId_Code",
                table: "Roles",
                columns: new[] { "ClientId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ClientId_Status",
                table: "Subscriptions",
                columns: new[] { "ClientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_EndsAt",
                table: "Subscriptions",
                column: "EndsAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGateAssignments_GateId",
                table: "UserGateAssignments",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGateAssignments_UserId_GateId",
                table: "UserGateAssignments",
                columns: new[] { "UserId", "GateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_SiteId",
                table: "UserRoles",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId_SiteId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId", "SiteId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessEvents_Clients_ClientId",
                table: "AccessEvents",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessEvents_HardwareDevices_DeviceId",
                table: "AccessEvents",
                column: "DeviceId",
                principalTable: "HardwareDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessEvents_Users_ActorUserId",
                table: "AccessEvents",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Lanes_Clients_ClientId",
                table: "Lanes",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Clients_ClientId",
                table: "Vehicles",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessEvents_Clients_ClientId",
                table: "AccessEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_AccessEvents_HardwareDevices_DeviceId",
                table: "AccessEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_AccessEvents_Users_ActorUserId",
                table: "AccessEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Lanes_Clients_ClientId",
                table: "Lanes");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Clients_ClientId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DeviceHeartbeats");

            migrationBuilder.DropTable(
                name: "GateCommands");

            migrationBuilder.DropTable(
                name: "ManualOverrides");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "UserGateAssignments");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "HardwareDevices");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_VisitorPasses_SiteId_ValidUntil",
                table: "VisitorPasses");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_ClientId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_IsBlacklisted_IsActive",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Users_ClientId_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Lanes_ClientId",
                table: "Lanes");

            migrationBuilder.DropIndex(
                name: "IX_Lanes_SiteId_Code",
                table: "Lanes");

            migrationBuilder.DropIndex(
                name: "IX_Clients_Status",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_AccessEvents_ActorUserId",
                table: "AccessEvents");

            migrationBuilder.DropIndex(
                name: "IX_AccessEvents_ClientId_CreatedAt",
                table: "AccessEvents");

            migrationBuilder.DropIndex(
                name: "IX_AccessEvents_DeviceId",
                table: "AccessEvents");

            migrationBuilder.DropIndex(
                name: "IX_AccessEvents_EventType",
                table: "AccessEvents");

            migrationBuilder.DropIndex(
                name: "IX_AccessEvents_LaneId_EventType_CreatedAt",
                table: "AccessEvents");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "VisitorPasses");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "VisitorPasses");

            migrationBuilder.DropColumn(
                name: "VehiclePlate",
                table: "VisitorPasses");

            migrationBuilder.DropColumn(
                name: "BlacklistReason",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsBlacklisted",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "OwnerPhone",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "OwnerPhone",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Lanes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Lanes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Lanes");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Lanes");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "AccessEvents");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "AccessEvents");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "AccessEvents");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "AccessEvents");

            migrationBuilder.DropColumn(
                name: "OpenMethod",
                table: "AccessEvents");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorPasses_SiteId",
                table: "VisitorPasses",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ClientId",
                table: "Users",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Lanes_SiteId",
                table: "Lanes",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessEvents_LaneId",
                table: "AccessEvents",
                column: "LaneId");
        }
    }
}
