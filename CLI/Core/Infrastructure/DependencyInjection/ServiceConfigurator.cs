using GrokCLI.Core.Settings;
using GrokCLI.Core.Services;
using GrokCLI.Core.Handlers;
using GrokCLI.Core.Infrastructure.Http;
using GrokCLI.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrokCLI.Core.Infrastructure.DependencyInjection
{
    public static class ServiceConfigurator
    {
        public static ServiceProvider Configure()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<GrokCLI.Core.Settings.GrokApiSettings>()
                .AddEnvironmentVariables()
                .Build();

            var grokApiSettings = config.GetSection("GrokApi").Get<GrokApiSettings>()!;
            var chatSettings = config.GetSection("ChatSettings").Get<ChatSettings>()!;

            // Try to get API key from environment variable first
            var apiKey = Environment.GetEnvironmentVariable("GROK_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Try to get from dotnet user-secrets
                apiKey = config["GrokApi:ApiKey"];
            }

            var services = new ServiceCollection();
            services.AddSingleton(grokApiSettings);
            services.AddSingleton(chatSettings);
            services.AddSingleton<IBankingDataService, BankingDataService>();
            services.AddSingleton<IFunctionDiscoveryService, FunctionDiscoveryService>();
            services.AddSingleton<IFunctionHandler, FunctionHandler>();
            services.AddSingleton(new GrokApiClient(new HttpClient(), grokApiSettings, apiKey ?? string.Empty));
            services.AddSingleton(new ChatService(chatSettings));
            return services.BuildServiceProvider();
        }
    }
}
