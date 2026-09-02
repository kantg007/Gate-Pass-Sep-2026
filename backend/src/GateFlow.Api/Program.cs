using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GateFlow.Application.DependencyInjection;
using GateFlow.Infrastructure.DependencyInjection;
using GateFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GateFlow API",
        Version = "v1",
        Description = """
            PARK+ style multi-tenant boom-barrier / parking access API.

            **How to try authenticated APIs**
            1. Call `POST /v1/auth/login` (demo: `client@greenvalley.local` / `Client@123`)
            2. Click **Authorize** → paste the JWT token (Swagger adds Bearer automatically)
            3. Call Sites / Vehicles / Reports endpoints

            **Device API** (`POST /v1/access/check`) uses header `X-Device-Key`
            (demo: `dev_demo_lane_key_001`) — no JWT.
            """,
        Contact = new OpenApiContact { Name = "GateFlow", Email = "admin@gateflow.local" },
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT from /v1/auth/login. Paste token only — Swagger adds Bearer prefix.",
    });

    options.AddSecurityDefinition("DeviceKey", new OpenApiSecurityScheme
    {
        Name = "X-Device-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Hardware / lane device API key (demo: dev_demo_lane_key_001)",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GateFlow API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "GateFlow API";
    c.DisplayRequestDuration();
    c.EnableTryItOutByDefault();
    c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
});
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
