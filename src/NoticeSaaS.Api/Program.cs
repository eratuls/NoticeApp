using NoticeSaaS.Infrastructure;
using NoticeSaaS.Infrastructure.Auth;
using NoticeSaaS.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddOptionalAzureKeyVault();

var appInsightsConnection = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConnection))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
        options.ConnectionString = appInsightsConnection);
}

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AngularOrigins").Get<string[]>()
        ??
        [
            "http://localhost:4200",
            "https://localhost:4200",
            "http://localhost:8088"
        ];

    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.Services.InitializeDatabaseAsync();

if (!app.Environment.IsEnvironment("Production")
    || !string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}

app.UseCors("AngularDev");
app.UseAuthentication();
app.UseMiddleware<SessionActivityMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
