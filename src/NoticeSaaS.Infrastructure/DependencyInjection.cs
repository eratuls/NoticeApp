using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NoticeSaaS.Application.Auth;
using NoticeSaaS.Application.Billing;
using NoticeSaaS.Application.Clients;
using NoticeSaaS.Application.Dashboard;
using NoticeSaaS.Application.Master;
using NoticeSaaS.Application.Notices;
using NoticeSaaS.Application.Notifications;
using NoticeSaaS.Application.Reminders;
using NoticeSaaS.Application.Sync;
using NoticeSaaS.Application.Team;
using NoticeSaaS.Infrastructure.Auth;
using NoticeSaaS.Infrastructure.Billing;
using NoticeSaaS.Infrastructure.Clients;
using NoticeSaaS.Infrastructure.Dashboard;
using NoticeSaaS.Infrastructure.Master;
using NoticeSaaS.Infrastructure.Notices;
using NoticeSaaS.Infrastructure.Notifications;
using NoticeSaaS.Infrastructure.Reminders;
using NoticeSaaS.Infrastructure.Sync;
using NoticeSaaS.Infrastructure.Team;
using NoticeSaaS.Infrastructure.Persistence;
using NoticeSaaS.Infrastructure.Persistence.Seed;

namespace NoticeSaaS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Default' is missing. Set ConnectionStrings:Default for the current environment.");
        }

        services.AddDbContext<NoticeSaaSDbContext>(options =>
            options.UseSqlServer(connectionString));

        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            keysPath = Path.Combine(Path.GetTempPath(), "NoticeSaaS", "dp-keys");
        }

        Directory.CreateDirectory(keysPath);
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("NoticeSaaS");

        return services;
    }

    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        services.AddSingleton<IIncomeTaxPortalClient, MockIncomeTaxPortalClient>();
        services.AddScoped<ISyncJobProcessor, SyncJobProcessor>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<IUsageLimitsService, UsageLimitsService>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddSyncServices();

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        if (string.IsNullOrWhiteSpace(authOptions.Jwt.SigningKey) || authOptions.Jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Auth:Jwt:SigningKey is missing or shorter than 32 characters.");
        }

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddSingleton<NoticeAttachmentStorage>();
        services.AddScoped<INoticeService, NoticeService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMasterDataService, MasterDataService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = authOptions.Jwt.Issuer,
                    ValidAudience = authOptions.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NoticeSaaSDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("NoticeSaaS.Database");
        var protector = scope.ServiceProvider
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("NoticeSaaS.PortalCredentials.v1");

        await db.Database.MigrateAsync(cancellationToken);
        await DatabaseSeeder.SeedAsync(db, logger, protector, cancellationToken);

        var attachmentStorage = scope.ServiceProvider.GetRequiredService<NoticeAttachmentStorage>();
        await DatabaseSeeder.SeedDemoNoticeAttachmentsAsync(db, attachmentStorage, logger, cancellationToken);
    }
}
