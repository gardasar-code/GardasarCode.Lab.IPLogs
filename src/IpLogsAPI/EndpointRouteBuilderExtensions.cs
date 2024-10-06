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
                    await service.GetLastConnectionAsync(userId, ctx).ConfigureAwait(false))
            .WithDescription("Найти последнее подключение пользователя")
            .WithOpenApi();

        app.MapGet("/user/{userId}/ips",
                ([FromRoute] long userId, [FromServices] IIPLogsService service, CancellationToken ctx) => service.GetUserIPsStream(userId, ctx))
            .WithDescription("Найти все IP-адреса пользователя")
            .WithOpenApi();

        app.MapGet("/search/{ipPart}",
                ([FromRoute] string ipPart, [FromServices] IIPLogsService service, CancellationToken ctx) =>
                    service.FindUsersByIpPartStream(ipPart, ctx))
            .WithDescription("Поиск пользователей по части IP адреса").WithOpenApi();
    }
}