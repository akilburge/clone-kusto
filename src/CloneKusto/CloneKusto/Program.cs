using System;
using System.Threading;
using System.Threading.Tasks;
using CloneKusto.Services;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloneKusto
{
    class Program
    {
        static Task Main(string[] args) => Parser.Default.ParseArguments<CloneOptions>(args).WithParsedAsync(RunAsync);

        private static async Task RunAsync(CloneOptions options)
        {
            var configuration = BuildConfiguration();
            var serviceProvider = ConfigureServices(new ServiceCollection(), configuration, options);

            var cloneKustoService = serviceProvider.GetService<ICloneKustoService>();

            try
            {
                await cloneKustoService.CloneAsync(options, CancellationToken.None);
            }
            catch (Exception ex)
            {
                var log = serviceProvider.GetService<ILogger<Program>>();
                log.LogError(ex, "Oops! Something went wrong...");
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder();

#if DEBUG
            builder
                .AddUserSecrets<Program>(optional: true);
#endif

            return builder.Build();
        }

        private static ServiceProvider ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration,
            CloneOptions options)
        {
            services
                .AddLogging(cfg => cfg.AddConsole())
                .Configure<LoggerFilterOptions>(o => o.MinLevel = options.Verbose ? LogLevel.Debug : LogLevel.Information);

            services
                .AddTransient<IKustoClientProvider, KustoClientProvider>(
                    sp => new KustoClientProvider(
                        options.ClusterUri ?? configuration["CloneOptions:ClusterUri"],
                        options.ApplicationClientId ?? configuration["CloneOptions:ApplicationClientId"],
                        options.ApplicationClientSecret ?? configuration["CloneOptions:ApplicationClientSecret"],
                        options.ApplicationCertificateThumbprint ?? configuration["CloneOptions:ApplicationCertificateThumbprint"],
                        options.TenantId ?? configuration["CloneOptions:TenantId"]))
                .AddTransient<ICloneKustoService, CloneKustoService>();

            return services.BuildServiceProvider();
        }
    }
}
