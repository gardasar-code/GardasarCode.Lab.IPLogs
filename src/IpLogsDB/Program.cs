using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IpLogsCommon.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace IpLogsDB;

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

            services.AddDbContext<IpLogsDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("db"));
                options.EnableSensitiveDataLogging();
            });

#pragma warning disable ASP0000
            _serviceProvider = services.BuildServiceProvider();
#pragma warning restore ASP0000

            #endregion

            var logger = _serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(IpLogsDB));

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

            var context = _serviceProvider.GetService<IpLogsDbContext>();

            if (context != null)
                await context.Database.MigrateAsync(Cts.Token).ConfigureAwait(false);
        }
        finally
        {
            Cts.Dispose();
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }
}