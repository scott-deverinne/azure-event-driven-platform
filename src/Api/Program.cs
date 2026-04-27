using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Queue: {builder.Configuration["ServiceBus:QueueName"]}");

// Load environment-specific configuration files
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

// Registers MVC controllers for API endpoints
builder.Services.AddControllers();

// Enables OpenAPI/Swagger endpoint discovery
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adds Application Insights for request telemetry and monitoring
builder.Services.AddApplicationInsightsTelemetry();

// enables ILogger → App Insights (this is what you're missing)
builder.Logging.AddApplicationInsights();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// -----------------------------
// Configuration Setup
// -----------------------------

var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
var serviceBusConnectionString = builder.Configuration["ServiceBusConnection"];

// Prefer direct configuration first
if (string.IsNullOrWhiteSpace(serviceBusConnectionString) &&
    !string.IsNullOrWhiteSpace(keyVaultUri))
{
    try
    {
        var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
        var secret = secretClient.GetSecret("ServiceBusConnection");
        serviceBusConnectionString = secret.Value.Value;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Key Vault access failed: {ex.Message}");
    }
}

// Fail fast if missing
if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    throw new InvalidOperationException(
        "Service Bus connection string is not configured. " +
        "Set 'ServiceBusConnection' in App Settings or configure Key Vault access.");
}

// -----------------------------
// Dependency Injection
// -----------------------------

builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));

// -----------------------------
// Build App
// -----------------------------

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); // leave off for Azure

app.UseAuthorization();

app.MapControllers();

app.Run();