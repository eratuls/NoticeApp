using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NoticeSaaS.Infrastructure.Persistence;

public class NoticeSaaSDbContextFactory : IDesignTimeDbContextFactory<NoticeSaaSDbContext>
{
    public NoticeSaaSDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var apiPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "NoticeSaaS.Api"));
        if (!Directory.Exists(apiPath))
        {
            apiPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "NoticeSaaS.Api"));
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        var options = new DbContextOptionsBuilder<NoticeSaaSDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new NoticeSaaSDbContext(options);
    }
}
