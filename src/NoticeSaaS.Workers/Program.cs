using NoticeSaaS.Infrastructure;
using NoticeSaaS.Workers;

var builder = Host.CreateApplicationBuilder(args);

var appInsightsConnection = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConnection))
{
    builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
        options.ConnectionString = appInsightsConnection);
}

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSyncServices();
builder.Services.AddHostedService<IncomeTaxSyncWorker>();

var host = builder.Build();
host.Run();
