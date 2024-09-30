using System.Threading;
using IpLogsCommon.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace IpLogsAPI;

public static class EndpointRouteBuilderExtensions
{
    public static void RegisterFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/user/{userId}/last-connection",
                async ([FromRoute] long userId, [FromServices] IIPLogsService service, CancellationToken ctx) =>
                await service.GetLastConnection(userId, ctx))
            .WithDescription("Найти последнее подключение пользователя")
            .WithOpenApi();

        app.MapGet("/user/{userId}/ips",
                ([FromRoute] long userId, [FromServices] IIPLogsService service, CancellationToken ctx) =>
                    Results.Ok(service.GetUserIPs(userId, ctx))).WithDescription("Найти все IP-адреса пользователя")
            .WithOpenApi();

        app.MapGet("/search/{ipPart}",
                ([FromRoute] string ipPart, [FromServices] IIPLogsService service, CancellationToken ctx) =>
                    Results.Ok(service.FindUsersByIpPart(ipPart, ctx)))
            .WithDescription("Поиск пользователей по части IP адреса").WithOpenApi();
    }
}