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
                ([FromRoute] long userId, [FromServices] IIPLogsService service, CancellationToken ctx) =>
                service.GetLastConnectionAsync(userId, ctx))
            .WithDescription("Найти последнее подключение пользователя")
            .WithOpenApi();

        app.MapGet("/user/{userId}/ips",
                ([FromRoute] long userId, [FromServices] IIPLogsService service, CancellationToken ctx) =>
                    Results.Ok(service.GetUserIPsStream(userId, ctx))).WithDescription("Найти все IP-адреса пользователя")
            .WithOpenApi();

        app.MapGet("/search/{ipPart}",
                ([FromRoute] string ipPart, [FromServices] IIPLogsService service, CancellationToken ctx) =>
                    Results.Ok(service.FindUsersByIpPartStream(ipPart, ctx)))
            .WithDescription("Поиск пользователей по части IP адреса").WithOpenApi();
    }
}