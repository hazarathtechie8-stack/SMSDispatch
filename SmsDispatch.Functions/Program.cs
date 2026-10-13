using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmsDispatch.Functions.Data;
using SmsDispatch.Functions.Resilience;
using SmsDispatch.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        // ── EF Core → same Azure SQL / LocalDB table as the Web API ──────────
        services.AddDbContext<FunctionsDbContext>(opt =>
            opt.UseSqlServer(cfg["SmsDb"]));

        // ── Service Bus client (used by SmsPoller to publish pending rows) ────
        services.AddSingleton(_ =>
            new ServiceBusClient(cfg["ServiceBusConnection"]!));

        // ── HttpClient "WebApi" — PUT /api/messages/{id}/status ──────────────
        services
            .AddHttpClient("WebApi", c =>
            {
                c.BaseAddress = new Uri(cfg["WebApiBaseUrl"] ?? "http://localhost:5000/");
                c.Timeout     = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler((sp, _) =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>()
                               .CreateLogger("WebApiResilience");
                return ResiliencePolicies.WebApiPolicy(logger);
            });

        // ── HttpClient for ISmsApiClient → third-party SMS endpoint ──────────
        //TODO: Uncomment and configure the following lines to enable the third-party SMS API client once you have the actual API details. The current configuration is commented out for safety and to avoid accidental calls to a non-existent or placeholder API.
        //services
        //    .AddHttpClient<ISmsApiClient, ThirdPartySmsApiClient>(c =>
        //    {
        //        c.BaseAddress = new Uri(cfg["SmsApiBaseUrl"] ?? "https://sms-api.example.com/");
        //        c.DefaultRequestHeaders.Add("X-Api-Key", cfg["SmsApiKey"] ?? "");
        //        c.Timeout = TimeSpan.FromSeconds(10);
        //    })
        //    .AddPolicyHandler((sp, _) =>
        //    {
        //        var logger = sp.GetRequiredService<ILoggerFactory>()
        //                       .CreateLogger("SmsApiResilience");
        //        return ResiliencePolicies.SmsApiPolicy(logger);
        //    });

        // Mocked one to test the function without actually sending SMS messages. Replace with the real implementation when ready.
        services.AddSingleton<ISmsApiClient, MockSmsApiClient>();

    })
    .Build();

await host.RunAsync();
