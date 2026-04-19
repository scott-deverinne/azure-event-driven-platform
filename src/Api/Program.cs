using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Registers MVC controllers for API endpoints
builder.Services.AddControllers();

// Enables OpenAPI/Swagger endpoint discovery
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adds Application Insights for request telemetry and monitoring
builder.Services.AddApplicationInsightsTelemetry();

// -----------------------------
// Configuration Setup
// -----------------------------

// Read configuration values (supports appsettings + environment variables)
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
var serviceBusConnectionString = builder.Configuration["ServiceBusConnection"];

// Prefer direct configuration (App Service setting) first
// This avoids startup failures if Key Vault is misconfigured
if (string.IsNullOrWhiteSpace(serviceBusConnectionString) &&
    !string.IsNullOrWhiteSpace(keyVaultUri))
{
    try
    {
        // Use Managed Identity in Azure, local credentials in dev
        var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

        // Retrieve Service Bus connection string from Key Vault
        var secret = secretClient.GetSecret("ServiceBusConnection");
        serviceBusConnectionString = secret.Value.Value;
    }
    catch (Exception ex)
    {
        // Log but do not crash immediately — fallback handled below
        Console.WriteLine($"Key Vault access failed: {ex.Message}");
    }
}

// Fail fast if no Service Bus connection string is available
if (string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    throw new InvalidOperationException(
        "Service Bus connection string is not configured. " +
        "Set 'ServiceBusConnection' in App Settings or configure Key Vault access.");
}

// -----------------------------
// Dependency Injection
// -----------------------------

// Register ServiceBusClient as a singleton for efficient reuse
builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));

// -----------------------------
// Build App
// -----------------------------

var app = builder.Build();

// Enable Swagger in all environments (useful for demo + debugging in Azure)
app.UseSwagger();
app.UseSwaggerUI();

// NOTE: HTTPS redirection can cause issues in Azure App Service Linux
// because the internal port is HTTP (8080). Safe to disable for now.
// app.UseHttpsRedirection();

app.UseAuthorization();

// Map controller routes
app.MapControllers();

// Start the application
app.Run();