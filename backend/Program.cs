using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GateFlow.Api.Data;
using GateFlow.Api.Models;
using GateFlow.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<AuthService>();

var dbSection = builder.Configuration.GetSection("Database");
var provider = dbSection["Provider"] ?? "Sqlite";
var sqliteCs = dbSection.GetSection("ConnectionStrings")["Sqlite"] ?? "Data Source=gateflow.db";
var sqlServerCs = dbSection.GetSection("ConnectionStrings")["SqlServer"];

builder.Services.AddDbContext<GateFlowDbContext>(options =>
{
    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(sqlServerCs);
    else
        options.UseSqlite(sqliteCs);
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "GateFlowDevSecretKey_ChangeMe_32chars!!";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "gateflow",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "gateflow",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",
        };
    });
builder.Services.AddAuthorization();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://127.0.0.1:5173", "http://localhost:5173"];
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
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
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Json(new { ok = true, service = "gateflow-api", runtime = ".NET" }));

// —— Auth (public) ——
app.MapPost("/v1/auth/register", async (RegisterBody body, AuthService auth) =>
{
    if (string.IsNullOrWhiteSpace(body.CompanyName) ||
        string.IsNullOrWhiteSpace(body.FullName) ||
        string.IsNullOrWhiteSpace(body.Email) ||
        string.IsNullOrWhiteSpace(body.Password))
    {
        return Results.BadRequest(new { error = "INVALID_BODY" });
    }

    var (ok, error, payload) = await auth.RegisterClientAsync(
        body.CompanyName, body.FullName, body.Email, body.Password, body.Phone);
    return ok ? Results.Json(payload, statusCode: 201) : Results.Conflict(new { error });
});

app.MapPost("/v1/auth/login", async (LoginBody body, AuthService auth) =>
{
    var (ok, error, payload) = await auth.LoginAsync(body.Email, body.Password);
    return ok ? Results.Json(payload) : Results.Json(new { error }, statusCode: 401);
});

app.MapGet("/v1/auth/me", async (ClaimsPrincipal principal, GateFlowDbContext db) =>
{
    var id = CurrentUser.UserId(principal);
    if (id is null) return Results.Unauthorized();
    var user = await db.Users.Include(u => u.Client).FirstOrDefaultAsync(u => u.Id == id);
    if (user is null) return Results.Unauthorized();
    return Results.Json(new
    {
        user.Id,
        user.Email,
        user.FullName,
        user.Role,
        user.ClientId,
        user.SiteId,
        client = user.Client == null ? null : new { user.Client.Id, user.Client.Name, user.Client.Status },
    });
}).RequireAuthorization();

// —— Platform admin: all clients ——
app.MapGet("/v1/admin/clients", async (ClaimsPrincipal user, GateFlowDbContext db) =>
{
    if (!CurrentUser.IsPlatformAdmin(user)) return Results.Forbid();
    var clients = await db.Clients
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new
        {
            c.Id,
            c.Name,
            c.ContactEmail,
            c.Phone,
            c.Status,
            c.CreatedAt,
            siteCount = c.Sites.Count,
            userCount = c.Users.Count,
        })
        .ToListAsync();
    return Results.Json(clients);
}).RequireAuthorization();

app.MapPatch("/v1/admin/clients/{clientId}/status", async (
    string clientId, StatusBody body, ClaimsPrincipal user, GateFlowDbContext db) =>
{
    if (!CurrentUser.IsPlatformAdmin(user)) return Results.Forbid();
    var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
    if (client is null) return Results.NotFound(new { error = "NOT_FOUND" });
    client.Status = body.Status;
    await db.SaveChangesAsync();
    return Results.Json(new { client.Id, client.Status });
}).RequireAuthorization();

// —— Client creates / lists their sites ——
app.MapGet("/v1/sites", async (ClaimsPrincipal user, GateFlowDbContext db) =>
{
    var q = db.Sites.AsQueryable();
    if (CurrentUser.IsPlatformAdmin(user))
    {
        // platform sees all
    }
    else
    {
        var clientId = CurrentUser.ClientId(user);
        if (clientId is null) return Results.Forbid();
        q = q.Where(s => s.ClientId == clientId);
        var siteScope = CurrentUser.SiteId(user);
        if (!string.IsNullOrEmpty(siteScope))
            q = q.Where(s => s.Id == siteScope);
    }

    var sites = await q.OrderBy(s => s.CreatedAt)
        .Select(s => new
        {
            s.Id,
            s.ClientId,
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
}).RequireAuthorization();

app.MapPost("/v1/sites", async (CreateSiteBody body, ClaimsPrincipal user, GateFlowDbContext db) =>
{
    var role = CurrentUser.Role(user);
    if (role is not (Roles.PlatformAdmin or Roles.ClientAdmin)) return Results.Forbid();

    var clientId = body.ClientId;
    if (role == Roles.ClientAdmin)
    {
        clientId = CurrentUser.ClientId(user);
    }
    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Slug))
    {
        return Results.BadRequest(new { error = "INVALID_BODY" });
    }

    var site = new Site
    {
        ClientId = clientId!,
        Name = body.Name.Trim(),
        Slug = body.Slug.Trim().ToLowerInvariant(),
        Settings = new SiteSettings().ToJson(),
    };
    db.Sites.Add(site);
    db.Lanes.Add(new Lane
    {
        SiteId = site.Id,
        Name = "Main Entry",
        Direction = "ENTRY",
        DeviceApiKey = $"dev_{Guid.NewGuid():N}"[..24],
    });
    await db.SaveChangesAsync();
    return Results.Json(new { site.Id, site.Name, site.Slug, site.ClientId }, statusCode: 201);
}).RequireAuthorization();

app.MapGet("/v1/sites/{siteId}", async (string siteId, ClaimsPrincipal user, GateFlowDbContext db) =>
{
    var site = await db.Sites.Include(s => s.Lanes).Include(s => s.Units).FirstOrDefaultAsync(s => s.Id == siteId);
    if (site is null) return Results.NotFound(new { error = "NOT_FOUND" });
    if (!CanAccessSite(user, site)) return Results.Forbid();

    return Results.Json(new
    {
        site.Id,
        site.ClientId,
        site.Name,
        site.Slug,
        site.IsActive,
        site.CreatedAt,
        site.UpdatedAt,
        settings = SiteSettings.Parse(site.Settings),
        lanes = site.Lanes,
        units = site.Units.Select(u => new { u.Id, u.Label, u.Block, u.Floor }),
    });
}).RequireAuthorization();

app.MapGet("/v1/sites/{siteId}/vehicles", async (string siteId, ClaimsPrincipal user, GateFlowDbContext db) =>
{
    if (!await CanAccessSiteId(user, db, siteId)) return Results.Forbid();
    var vehicles = await db.Vehicles.Where(v => v.SiteId == siteId)
        .Include(v => v.Unit).Include(v => v.Credentials)
        .OrderBy(v => v.PlateNumber)
        .Select(v => new
        {
            v.Id,
            v.PlateNumber,
            v.Label,
            v.IsActive,
            unit = v.Unit == null ? null : new { v.Unit.Label },
            credentials = v.Credentials.Select(c => new { c.Id, c.Type, c.Code }),
        }).ToListAsync();
    return Results.Json(vehicles);
}).RequireAuthorization();

app.MapPost("/v1/sites/{siteId}/vehicles", async (string siteId, CreateVehicleBody body, ClaimsPrincipal user, GateFlowDbContext db) =>
{
    if (!await CanAccessSiteId(user, db, siteId)) return Results.Forbid();
    if (CurrentUser.Role(user) == Roles.Guard) return Results.Forbid();
    if (string.IsNullOrWhiteSpace(body.PlateNumber)) return Results.BadRequest(new { error = "INVALID_BODY" });

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
        var c = new AccessCredential { SiteId = siteId, Type = "RFID", Code = body.RfidCode.Trim(), VehicleId = vehicle.Id };
        db.AccessCredentials.Add(c); creds.Add(c);
    }
    if (!string.IsNullOrWhiteSpace(body.BarcodeCode))
    {
        var c = new AccessCredential { SiteId = siteId, Type = "BARCODE", Code = body.BarcodeCode.Trim(), VehicleId = vehicle.Id };
        db.AccessCredentials.Add(c); creds.Add(c);
    }
    await db.SaveChangesAsync();
    return Results.Json(new { vehicle, credentials = creds }, statusCode: 201);
}).RequireAuthorization();

app.MapPost("/v1/sites/{siteId}/visitors", async (string siteId, CreateVisitorBody body, ClaimsPrincipal user, GateFlowDbContext db) =>
{
    if (!await CanAccessSiteId(user, db, siteId)) return Results.Forbid();
    var site = await db.Sites.FirstAsync(s => s.Id == siteId);
    if (string.IsNullOrWhiteSpace(body.GuestName)) return Results.BadRequest(new { error = "INVALID_BODY" });

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
    db.AccessCredentials.Add(new AccessCredential
    {
        SiteId = siteId,
        Type = "QR",
        Code = qrCode,
        VisitorPassId = pass.Id,
        ExpiresAt = validUntil,
    });
    await db.SaveChangesAsync();
    return Results.Json(new { visitorPass = pass, qrPayload = qrCode }, statusCode: 201);
}).RequireAuthorization();

app.MapGet("/v1/sites/{siteId}/events", async (string siteId, ClaimsPrincipal user, GateFlowDbContext db, int? limit) =>
{
    if (!await CanAccessSiteId(user, db, siteId)) return Results.Forbid();
    var take = Math.Min(limit ?? 50, 200);
    var events = await db.AccessEvents.Where(e => e.SiteId == siteId)
        .Include(e => e.Lane).OrderByDescending(e => e.CreatedAt).Take(take).ToListAsync();
    return Results.Json(events.Select(e => new
    {
        e.Id, e.Decision, e.Reason, e.CredentialType, e.CredentialCode, e.PlateNumber, e.CreatedAt,
        lane = e.Lane == null ? null : new { e.Lane.Name },
        meta = SafeJson(e.Meta),
    }));
}).RequireAuthorization();

app.MapGet("/v1/sites/{siteId}/lanes", async (string siteId, ClaimsPrincipal user, GateFlowDbContext db) =>
{
    if (!await CanAccessSiteId(user, db, siteId)) return Results.Forbid();
    var lanes = await db.Lanes.Where(l => l.SiteId == siteId).ToListAsync();
    return Results.Json(lanes.Select(l => new
    {
        l.Id, l.Name, l.Direction, l.DeviceApiKey, l.IsActive, config = SafeJson(l.Config),
    }));
}).RequireAuthorization();

// Device path — no JWT, device key only (Park+ style controller)
app.MapPost("/v1/access/check", async (AccessCheckBody body, HttpRequest req, AccessService access) =>
{
    if (string.IsNullOrWhiteSpace(body.CredentialType) || string.IsNullOrWhiteSpace(body.Code))
        return Results.BadRequest(new { error = "INVALID_BODY" });

    var deviceKey = req.Headers["X-Device-Key"].FirstOrDefault();
    var result = await access.CheckAsync(new AccessCheckRequest(
        body.CredentialType, body.Code, body.SiteId, body.LaneId, deviceKey, body.Meta));
    return result.Open ? Results.Json(result) : Results.Json(result, statusCode: 403);
});

app.Run();

static bool CanAccessSite(ClaimsPrincipal user, Site site)
{
    if (CurrentUser.IsPlatformAdmin(user)) return true;
    var clientId = CurrentUser.ClientId(user);
    if (clientId is null || site.ClientId != clientId) return false;
    var siteScope = CurrentUser.SiteId(user);
    if (!string.IsNullOrEmpty(siteScope) && siteScope != site.Id) return false;
    return true;
}

static async Task<bool> CanAccessSiteId(ClaimsPrincipal user, GateFlowDbContext db, string siteId)
{
    var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == siteId);
    return site is not null && CanAccessSite(user, site);
}

static object SafeJson(string raw)
{
    try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(raw) ?? new(); }
    catch { return new Dictionary<string, object?>(); }
}

record RegisterBody(string CompanyName, string FullName, string Email, string Password, string? Phone);
record LoginBody(string Email, string Password);
record StatusBody(string Status);
record CreateSiteBody(string Name, string Slug, string? ClientId);
record AccessCheckBody(string CredentialType, string Code, string? SiteId, string? LaneId, Dictionary<string, object?>? Meta);
record CreateVehicleBody(string PlateNumber, string? Label, string? UnitId, string? RfidCode, string? BarcodeCode);
record CreateVisitorBody(string GuestName, string? UnitId, string? Purpose, int? MaxUses, int? ValidHours);
