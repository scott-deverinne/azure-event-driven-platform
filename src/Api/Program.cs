using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Registers MVC controllers for API endpoints
builder.Services.AddControllers();

// Enables OpenAPI/Swagger endpoint discovery
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adds Application Insights for request telemetry, dependency tracking,
// and centralized monitoring across the API
builder.Services.AddApplicationInsightsTelemetry();

// Retrieves Key Vault URI and queue name from configuration
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
var queueName = builder.Configuration["ServiceBus:QueueName"];

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    // DefaultAzureCredential allows local development using developer identity
    // and automatically switches to Managed Identity when deployed in Azure
    var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

    // Retrieves the Service Bus connection string securely from Key Vault
    var secret = secretClient.GetSecret("ServiceBusConnection");
    var serviceBusConnectionString = secret.Value.Value;

    if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
    {
        // Registers ServiceBusClient using a securely retrieved secret instead of config
        // Ensures efficient connection reuse and removes hard-coded credentials
        builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));
    }
}

var app = builder.Build();

// ✅ Enable Swagger for ALL environments (including Azure)
// This ensures the API documentation is accessible after deployment
app.UseSwagger();
app.UseSwaggerUI();

// Redirects HTTP traffic to HTTPS
//app.UseHttpsRedirection();

// Adds authorization middleware for future secured endpoints
app.UseAuthorization();

// Maps controller routes
app.MapControllers();

// Temporary probe endpoint to verify the deployed app is serving requests
app.MapGet("/health", () => Results.Ok("API is running"));

// Starts the application
app.Run();