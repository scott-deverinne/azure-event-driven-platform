using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        / log
        var environment = context.HostingEnvironment.EnvironmentName;
        Console.WriteLine($"Running in environment: {environment}");
        // Load base and environment-specific configuration files first
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
              .AddJsonFile(
                  $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                  optional: true,
                  reloadOnChange: false)
              .AddEnvironmentVariables();

        var builtConfig = config.Build();
        var keyVaultUri = builtConfig["KeyVault:VaultUri"];

        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            // Loads secrets from Key Vault so bindings can resolve them from configuration
            config.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();