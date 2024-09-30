using IpLogsCommon.Models;

namespace IpLogsCommon.Interfaces;

public interface IIPLogsService
{
    /// <summary>
    ///     Добавить новое событие подключения
    /// </summary>
    /// <param name="userId">уникальный идентификатор пользователя</param>
    /// <param name="ipAddress">IP-адрес, с которого подключался пользователь</param>
    /// <param name="eventTime">время подключения пользователя</param>
    /// <param name="cancellationToken">уведомление, если оперция должна быть отменена</param>
    Task AddConnection(long userId, string ipAddress, DateTime eventTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Найти последнее подключение пользователя
    /// </summary>
    /// <param name="userId">уникальный идентификатор пользователя</param>
    /// <param name="cancellationToken">уведомление, если оперция должна быть отменена</param>
    /// <returns>время последнего подключения пользователя и IP-адрес, с которого подключался пользователь</returns>
    public Task<UserLastConnection> GetLastConnection(long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Найти все IP-адреса пользователя
    /// </summary>
    /// <param name="userId">уникальный идентификатор пользователя</param>
    /// <param name="cancellationToken">уведомление, если оперция должна быть отменена</param>
    /// <returns></returns>
    public IAsyncEnumerable<string> GetUserIPs(long userId, CancellationToken cancellationToken);

    // Поиск пользователей по части IP адреса
    /// <summary>
    ///     Поиск пользователей по части IP адреса
    /// </summary>
    /// <param name="ipPart">начальная часть IP адреса</param>
    /// <param name="cancellationToken">уведомление, если оперция должна быть отменена</param>
    /// <returns>уникальные идентификаторы пользователей</returns>
    public IAsyncEnumerable<long> FindUsersByIpPart(string ipPart, CancellationToken cancellationToken = default);
}