using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IpLogsCommon;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace IpLogsEventsGenerator;

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
            services.AddTransient<IEventProducer, EventProducer>();
            services.AddTransient<IEventGenerator, EventGenerator>();

#pragma warning disable ASP0000
            _serviceProvider = services.BuildServiceProvider();
#pragma warning restore ASP0000

            #endregion

            var logger = _serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(IpLogsEventsGenerator));

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

            var eventProducer = _serviceProvider.GetService<IEventProducer>();
            var eventGenerator = _serviceProvider.GetService<IEventGenerator>();

            while (!Cts.Token.IsCancellationRequested)
            {
                var eventMessage = eventGenerator?.Generate();
                if (eventMessage != null && eventProducer != null)
                    await eventProducer.ProduceAsync(eventMessage);
                await Task.Delay(1_000, Cts.Token);
            }
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}