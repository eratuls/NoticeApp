using Microsoft.Extensions.DependencyInjection;

namespace NoticeSaaS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Day 2+: EF Core, Blob, Key Vault registrations
        return services;
    }
}
