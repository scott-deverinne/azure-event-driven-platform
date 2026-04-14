using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

// Registers MVC controllers for API endpoints
builder.Services.AddControllers();

// Enables OpenAPI/Swagger endpoint discovery
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Adds Application Insights for request telemetry, dependency tracking,
// and centralised monitoring across the API
builder.Services.AddApplicationInsightsTelemetry();

// Retrieves Service Bus connection string from configuration
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];

// Registers ServiceBusClient as a singleton so the API can publish
// messages efficiently without recreating connections per request
if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
{
    builder.Services.AddSingleton(_ => new ServiceBusClient(serviceBusConnectionString));
}

var app = builder.Build();

// Enables Swagger UI during development for endpoint testing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirects HTTP traffic to HTTPS
app.UseHttpsRedirection();

// Adds authorization middleware for future secured endpoints
app.UseAuthorization();

// Maps controller routes
app.MapControllers();

// Starts the application
app.Run();