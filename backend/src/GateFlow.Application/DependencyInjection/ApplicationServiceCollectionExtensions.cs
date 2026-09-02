using Microsoft.Extensions.DependencyInjection;

namespace GateFlow.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Application-layer registrations (none yet — services live in Infrastructure).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
