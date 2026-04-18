using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
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