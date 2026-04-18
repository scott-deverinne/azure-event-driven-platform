using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

// Add Key Vault (optional if already added)
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// Registers MVC controllers for API endpoints
builder.Services.AddControllers();

// Enables OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Service Bus setup (from Key Vault or config)
var serviceBusConnectionString = builder.Configuration["ServiceBusConnection"];

if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));
}

var app = builder.Build();

// ✅ Enable Swagger in ALL environments (important for Azure)
app.UseSwagger();
app.UseSwaggerUI();

// Redirect HTTP traffic to HTTPS
app.UseHttpsRedirection();

// Authorization middleware
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Run app
app.Run();