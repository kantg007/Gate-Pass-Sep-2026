using GateFlow.Application.Abstractions;
using GateFlow.Infrastructure.Persistence;
using GateFlow.Infrastructure.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GateFlow.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var dbSection = config.GetSection("Database");
        var provider = dbSection["Provider"] ?? "Sqlite";
        var sqliteCs = dbSection.GetSection("ConnectionStrings")["Sqlite"] ?? "Data Source=gateflow.db";
        var sqlServerCs = dbSection.GetSection("ConnectionStrings")["SqlServer"];

        services.AddDbContext<GateFlowDbContext>(options =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
                options.UseSqlServer(sqlServerCs, sql => sql.MigrationsAssembly(typeof(GateFlowDbContext).Assembly.FullName));
            else
                options.UseSqlite(sqliteCs, sqlite => sqlite.MigrationsAssembly(typeof(GateFlowDbContext).Assembly.FullName));
        });

        services.AddScoped<IAuthService, AuthHandler>();
        services.AddScoped<IAccessService, AccessCheckHandler>();

        return services;
    }
}
