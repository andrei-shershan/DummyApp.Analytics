using DummyApp.Analytics.Functions.Extensions;
using DummyApp.Analytics.Functions.Options;
using DummyApp.Analytics.Functions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration(config => config.AddKeyVaultFromConfiguration())
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<CosmosDbOptions>()
            .Bind(context.Configuration.GetSection(CosmosDbOptions.SectionName));

        services.AddSingleton<ICosmosAnalyticsRepository, CosmosAnalyticsRepository>();
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
