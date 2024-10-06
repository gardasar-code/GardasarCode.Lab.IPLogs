using System.Threading.Tasks;
using GardasarCode.Repository;
using GardasarCode.Repository.Interfaces;
using IpLogsCommon;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models.Options;
using IpLogsCommon.Repository.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace IpLogsAPI;

public class Program
{
    private static async Task Main()
    {
        try
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddProblemDetails();
            
            builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("REDIS"));

            builder.Services.AddDbContext<IpLogsDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("db"));
                options.EnableSensitiveDataLogging();
            });
            builder.Services.AddTransient<IRepository, RepositoryBase<IpLogsDbContext>>();
            builder.Services.AddTransient<IIPLogsService, IPLogsService>();
            builder.Services.AddTransient<ICache, RedisCache>();

            builder.Host.UseSerilog((o, u) => u.ReadFrom.Configuration(o.Configuration).WriteTo.Console());

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseExceptionHandler();
            app.UseStatusCodePages();

            app.RegisterFileEndpoints();

            app.MapFallback(async Task (context) =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("The endpoint you are looking for does not exist!")
                    .ConfigureAwait(false);
            });

            await app.RunAsync().ConfigureAwait(false);
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }
}