using System.Text.Json;
using System.Text.Json.Serialization;
using GateFlow.Api.Data;
using GateFlow.Api.Models;
using GateFlow.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<AccessService>();

var dbSection = builder.Configuration.GetSection("Database");
var provider = dbSection["Provider"] ?? "Sqlite";
var sqliteCs = dbSection.GetSection("ConnectionStrings")["Sqlite"] ?? "Data Source=gateflow.db";
var sqlServerCs = dbSection.GetSection("ConnectionStrings")["SqlServer"];

builder.Services.AddDbContext<GateFlowDbContext>(options =>
{
    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(sqlServerCs);
    }
    else
    {
        options.UseSqlite(sqliteCs);
    }
});

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://127.0.0.1:5173", "http://localhost:5173"];

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p =>
        p.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GateFlowDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapGet("/health", () => Results.Json(new { ok = true, service = "gateflow-api", runtime = ".NET" }));

app.MapPost("/v1/access/check", async (AccessCheckBody body, HttpRequest req, AccessService access) =>
{
    if (string.IsNullOrWhiteSpace(body.CredentialType) || string.IsNullOrWhiteSpace(body.Code))
    {
        return Results.BadRequest(new { error = "INVALID_BODY" });
    }

    var deviceKey = req.Headers["X-Device-Key"].FirstOrDefault();
    var result = await access.CheckAsync(new AccessCheckRequest(
        body.CredentialType,
        body.Code,
        body.SiteId,
        body.LaneId,
        deviceKey,
        body.Meta));

    return result.Open ? Results.Json(result) : Results.Json(result, statusCode: 403);
});

app.MapGet("/v1/sites", async (GateFlowDbContext db) =>
{
    var sites = await db.Sites
        .OrderBy(s => s.CreatedAt)
        .Select(s => new
        {
            s.Id,
            s.Name,
            s.Slug,
            s.IsActive,
            s.CreatedAt,
            s.UpdatedAt,
            settings = SiteSettings.Parse(s.Settings),
            _count = new
            {
                vehicles = s.Vehicles.Count,
                lanes = s.Lanes.Count,
                events = s.Events.Count,
            },
        })
        .ToListAsync();
    return Results.Json(sites);
});

app.MapGet("/v1/sites/{siteId}", async (string siteId, GateFlowDbContext db) =>
{
    var site = await db.Sites
        .Include(s => s.Lanes)
        .Include(s => s.Units)
        .FirstOrDefaultAsync(s => s.Id == siteId);
    if (site is null) return Results.NotFound(new { error = "NOT_FOUND" });

    return Results.Json(new
    {
        site.Id,
        site.Name,
        site.Slug,
        site.IsActive,
        site.CreatedAt,
        site.UpdatedAt,
        settings = SiteSettings.Parse(site.Settings),
        lanes = site.Lanes,
        units = site.Units.Select(u => new { u.Id, u.Label, u.Block, u.Floor }),
    });
});

app.MapPatch("/v1/sites/{siteId}/settings", async (string siteId, JsonElement body, GateFlowDbContext db) =>
{
    var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId);
    if (site is null) return Results.NotFound(new { error = "NOT_FOUND" });

    var current = SiteSettings.Parse(site.Settings);
    var incoming = JsonSerializer.Deserialize<SiteSettings>(body.GetRawText(), new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    });
    if (incoming is not null)
    {
        current.AllowManualOpen = incoming.AllowManualOpen;
        current.VisitorDefaultMaxUses = incoming.VisitorDefaultMaxUses;
        current.VisitorDefaultValidHours = incoming.VisitorDefaultValidHours;
        current.RequireActiveVehicle = incoming.RequireActiveVehicle;
        current.DenyExpiredCredentials = incoming.DenyExpiredCredentials;
        current.LogDeniedAttempts = incoming.LogDeniedAttempts;
        foreach (var kv in incoming.Features)
        {
            current.Features[kv.Key] = kv.Value;
        }
    }

    site.Settings = current.ToJson();
    site.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Json(new { site.Id, settings = current });
});

app.MapGet("/v1/sites/{siteId}/vehicles", async (string siteId, GateFlowDbContext db) =>
{
    var vehicles = await db.Vehicles
        .Where(v => v.SiteId == siteId)
        .Include(v => v.Unit)
        .Include(v => v.Credentials)
        .OrderBy(v => v.PlateNumber)
        .Select(v => new
        {
            v.Id,
            v.PlateNumber,
            v.Label,
            v.IsActive,
            unit = v.Unit == null ? null : new { v.Unit.Label },
            credentials = v.Credentials.Select(c => new { c.Id, c.Type, c.Code }),
        })
        .ToListAsync();
    return Results.Json(vehicles);
});

app.MapPost("/v1/sites/{siteId}/vehicles", async (string siteId, CreateVehicleBody body, GateFlowDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(body.PlateNumber))
    {
        return Results.BadRequest(new { error = "INVALID_BODY" });
    }

    var vehicle = new Vehicle
    {
        SiteId = siteId,
        PlateNumber = body.PlateNumber.Trim().ToUpperInvariant(),
        Label = body.Label,
        UnitId = string.IsNullOrWhiteSpace(body.UnitId) ? null : body.UnitId,
    };
    db.Vehicles.Add(vehicle);

    var creds = new List<AccessCredential>();
    if (!string.IsNullOrWhiteSpace(body.RfidCode))
    {
        var c = new AccessCredential
        {
            SiteId = siteId,
            Type = "RFID",
            Code = body.RfidCode.Trim(),
            VehicleId = vehicle.Id,
        };
        db.AccessCredentials.Add(c);
        creds.Add(c);
    }
    if (!string.IsNullOrWhiteSpace(body.BarcodeCode))
    {
        var c = new AccessCredential
        {
            SiteId = siteId,
            Type = "BARCODE",
            Code = body.BarcodeCode.Trim(),
            VehicleId = vehicle.Id,
        };
        db.AccessCredentials.Add(c);
        creds.Add(c);
    }

    await db.SaveChangesAsync();
    return Results.Json(new { vehicle, credentials = creds }, statusCode: 201);
});

app.MapPost("/v1/sites/{siteId}/units", async (string siteId, CreateUnitBody body, GateFlowDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(body.Label))
    {
        return Results.BadRequest(new { error = "INVALID_BODY" });
    }

    var unit = new Unit
    {
        SiteId = siteId,
        Label = body.Label.Trim(),
        Block = body.Block,
        Floor = body.Floor,
    };
    db.Units.Add(unit);
    await db.SaveChangesAsync();
    return Results.Json(unit, statusCode: 201);
});

app.MapPost("/v1/sites/{siteId}/visitors", async (string siteId, CreateVisitorBody body, GateFlowDbContext db) =>
{
    var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId);
    if (site is null) return Results.NotFound(new { error = "NOT_FOUND" });
    if (string.IsNullOrWhiteSpace(body.GuestName))
    {
        return Results.BadRequest(new { error = "INVALID_BODY" });
    }

    var settings = SiteSettings.Parse(site.Settings);
    var hours = body.ValidHours ?? settings.VisitorDefaultValidHours;
    var maxUses = body.MaxUses ?? settings.VisitorDefaultMaxUses;
    var validUntil = DateTime.UtcNow.AddHours(hours);
    var qrCode = $"VIS-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

    var pass = new VisitorPass
    {
        SiteId = siteId,
        GuestName = body.GuestName.Trim(),
        UnitId = string.IsNullOrWhiteSpace(body.UnitId) ? null : body.UnitId,
        Purpose = body.Purpose,
        MaxUses = maxUses,
        ValidUntil = validUntil,
    };
    db.VisitorPasses.Add(pass);

    var credential = new AccessCredential
    {
        SiteId = siteId,
        Type = "QR",
        Code = qrCode,
        VisitorPassId = pass.Id,
        ExpiresAt = validUntil,
    };
    // Link after IDs exist — set navigation via FKs we already assigned
    db.AccessCredentials.Add(credential);
    // Fix circular: Credential needs VisitorPassId; VisitorPass.Id is set
    credential.VisitorPassId = pass.Id;

    await db.SaveChangesAsync();

    return Results.Json(new
    {
        visitorPass = pass,
        credential,
        qrPayload = qrCode,
    }, statusCode: 201);
});

app.MapGet("/v1/sites/{siteId}/events", async (string siteId, GateFlowDbContext db, int? limit) =>
{
    var take = Math.Min(limit ?? 50, 200);
    var events = await db.AccessEvents
        .Where(e => e.SiteId == siteId)
        .Include(e => e.Lane)
        .OrderByDescending(e => e.CreatedAt)
        .Take(take)
        .ToListAsync();

    return Results.Json(events.Select(e => new
    {
        e.Id,
        e.Decision,
        e.Reason,
        e.CredentialType,
        e.CredentialCode,
        e.PlateNumber,
        e.CreatedAt,
        lane = e.Lane == null ? null : new { e.Lane.Name },
        meta = SafeJson(e.Meta),
    }));
});

app.MapGet("/v1/sites/{siteId}/lanes", async (string siteId, GateFlowDbContext db) =>
{
    var lanes = await db.Lanes.Where(l => l.SiteId == siteId).ToListAsync();
    return Results.Json(lanes.Select(l => new
    {
        l.Id,
        l.Name,
        l.Direction,
        l.DeviceApiKey,
        l.IsActive,
        config = SafeJson(l.Config),
    }));
});

app.Run();

static object SafeJson(string raw)
{
    try
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(raw) ?? new Dictionary<string, object?>();
    }
    catch
    {
        return new Dictionary<string, object?>();
    }
}

record AccessCheckBody(
    string CredentialType,
    string Code,
    string? SiteId,
    string? LaneId,
    Dictionary<string, object?>? Meta);

record CreateVehicleBody(
    string PlateNumber,
    string? Label,
    string? UnitId,
    string? RfidCode,
    string? BarcodeCode);

record CreateUnitBody(string Label, string? Block, string? Floor);

record CreateVisitorBody(
    string GuestName,
    string? UnitId,
    string? Purpose,
    int? MaxUses,
    int? ValidHours);
