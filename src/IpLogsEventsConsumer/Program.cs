using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IpLogsCommon;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models.Options;
using IpLogsCommon.Repository;
using IpLogsCommon.Repository.Context;
using IpLogsCommon.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace IpLogsEventsConsumer;

public class Program
{
    private static IServiceProvider? _serviceProvider;
    private static readonly CancellationTokenSource Cts = new();

    private static async Task Main()
    {
        try
        {
            #region di

            var configurationBuilder = new ConfigurationBuilder();
            var configuration = configurationBuilder.AddEnvironmentVariables().Build();

            var services = new ServiceCollection();
            services.AddSerilog(o => o.ReadFrom.Configuration(configuration).WriteTo.Console()).AddLogging();

            services.Configure<KafkaOptions>(configuration.GetSection("KAFKA"));
            services.Configure<RedisOptions>(configuration.GetSection("REDIS"));

            services.AddDbContext<IpLogsDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("db"));
                options.EnableSensitiveDataLogging();
            });
            services.AddTransient<IRepository, RepositoryBase<IpLogsDbContext>>();

            services.AddTransient<IIPLogsService, IPLogsService>();
            services.AddTransient<IEventConsumer, EventConsumer>();
            services.AddTransient<ICache, RedisCache>();

#pragma warning disable ASP0000
            _serviceProvider = services.BuildServiceProvider();
#pragma warning restore ASP0000

            #endregion

            var logger = _serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(IpLogsEventsConsumer));

            #region terminate

            Console.CancelKeyPress += (_, e) =>
            {
                logger?.LogInformation("CTRL+C received. Terminating...");
                e.Cancel = true;
                Cts.Cancel();
            };

            using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                logger?.LogInformation("SIGTERM received. Terminating...");
                context.Cancel = false;
                Cts.Cancel();
            });

            #endregion

            logger?.LogInformation("Application started. Press CTRL+C to terminate.");

            var eventConsumer = _serviceProvider.GetService<IEventConsumer>();

            while (!Cts.Token.IsCancellationRequested)
                try
                {
                    if (eventConsumer != null)
                        await eventConsumer.ConsumeAsync(Cts.Token);
                    await Task.Delay(1_000, Cts.Token);
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Error occurred while consuming events.");
                }
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}