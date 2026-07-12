using NoticeSaaS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.Services.InitializeDatabaseAsync();
}

app.UseHttpsRedirection();
app.UseCors("AngularDev");
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
