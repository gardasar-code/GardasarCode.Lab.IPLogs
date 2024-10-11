using GardasarCode.Repository.Interfaces;
using IpLogsCommon;
using IpLogsCommon.Interfaces;
using IpLogsCommon.Models;
using IpLogsCommon.Repository.Entities;
using IpLogsCommon.Repository.Specifications;
using IpLogsTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace IpLogsTests;

public class UnitTestIpLogsService
{
    #region AddConnection

    [Fact]
    public async Task AddConnection_Should_AddNewUser_When_UserDoesNotExist()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        mockRepo.Setup<Task<User?>>(r =>
                r.FirstOrDefaultAsync(It.IsAny<UserSpecification.GetUserById>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        await service.AddConnectionAsync(1, "127.0.0.1", DateTime.UtcNow).ConfigureAwait(true);

        // Assert
        mockRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddConnection_Should_UpdateUser_When_UserExists()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        var existingUser = new User { Id = 1, IPAddress = "127.0.0.1", LastConnectionTime = DateTime.UtcNow };

        mockRepo.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<UserSpecification.GetUserById>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        await service.AddConnectionAsync(1, "192.168.0.1", DateTime.UtcNow);

        // Assert
        mockRepo.Verify(r => r.SetValues(existingUser, It.IsAny<User>()), Times.Once);
        mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Exactly(2));
    }

    #endregion

    #region GetLastConnection

    [Fact]
    public async Task GetLastConnection_Should_ReturnUserLastConnection_When_DB_IsAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        var dbuser = new User { Id = 1, IPAddress = "127.0.0.1", LastConnectionTime = DateTime.UtcNow };

        mockCache.Setup(c => c.TryGetValueAsync<UserLastConnection>(It.IsAny<string>())).ReturnsAsync((false, null));
        mockRepo.Setup(c => c.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbuser);

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var result = await service.GetLastConnectionAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dbuser.LastConnectionTime, result.LastConnectionTime);
        Assert.Equal(dbuser.IPAddress, result.IPAddress);

        mockCache.Verify(c => c.TryGetValueAsync<UserLastConnection>(It.IsAny<string>()), Times.Exactly(2));
        mockRepo.Verify(c => c.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserLastConnection>()), Times.Once);
    }

    [Fact]
    public async Task GetLastConnection_Should_ReturnUserLastConnection_When_Cache_IsAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        var cacheUser = new User { Id = 1, IPAddress = "127.0.0.1", LastConnectionTime = DateTime.UtcNow };

        mockCache.Setup(c => c.TryGetValueAsync<UserLastConnection>(It.IsAny<string>())).ReturnsAsync((true,
            new UserLastConnection
                { LastConnectionTime = cacheUser.LastConnectionTime, IPAddress = cacheUser.IPAddress }));

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var result = await service.GetLastConnectionAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cacheUser.LastConnectionTime, result.LastConnectionTime);
        Assert.Equal(cacheUser.IPAddress, result.IPAddress);

        mockCache.Verify(c => c.TryGetValueAsync<UserLastConnection>(It.IsAny<string>()), Times.Once);
        mockRepo.Verify(c => c.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserLastConnection>()), Times.Never);
    }

    [Fact]
    public async Task GetLastConnection_Should_ReturnUserLastConnection_When_Cache_IsNullAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        mockCache.Setup(c => c.TryGetValueAsync<UserLastConnection>(It.IsAny<string>())).ReturnsAsync((true, null));

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var result = await service.GetLastConnectionAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.LastConnectionTime);
        Assert.NotNull(result.IPAddress);
        Assert.Empty(result.IPAddress);

        mockCache.Verify(c => c.TryGetValueAsync<UserLastConnection>(It.IsAny<string>()), Times.Once);
        mockRepo.Verify(c => c.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserLastConnection>()), Times.Never);
    }
    
    [Fact]
    public async Task GetLastConnection_Should_ReturnUserLastConnectionWithEmpty_When_NoAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        mockCache.Setup(c => c.TryGetValueAsync<UserLastConnection>(It.IsAny<string>())).ReturnsAsync((false, null));
        mockRepo.Setup(c => c.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var result = await service.GetLastConnectionAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.LastConnectionTime);
        Assert.Null(result.IPAddress);

        mockCache.Verify(c => c.TryGetValueAsync<UserLastConnection>(It.IsAny<string>()), Times.Exactly(2));
        mockRepo.Verify(c => c.FirstOrDefaultAsync(It.IsAny<ISpecification<User>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserLastConnection>()), Times.Once);
    }

    #endregion

    #region GetUserIPs

    [Fact]
    public async Task GetUserIPs_Should_ReturnIPsFromCache_When_DB_IsAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        var dbIps = new List<string> { "127.0.0.1", "192.168.0.1" };

        mockCache.Setup(c => c.TryGetValueAsync<List<string>>(It.IsAny<string>())).ReturnsAsync((false, null));
        mockRepo.Setup(c =>
                c.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, string>>(), It.IsAny<CancellationToken>()))
            .Returns(dbIps.ToConfiguredCancelableAsyncEnumerable());

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var ips = await service.GetUserIPsStream(1).ToListAsync();

        // Assert
        Assert.Equal(dbIps, ips);
        mockCache.Verify(c => c.TryGetValueAsync<List<string>>(It.IsAny<string>()), Times.Exactly(2));
        mockRepo.Verify(
            r => r.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task GetUserIPs_Should_ReturnIPsFromCache_When_Cache_IsAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        var cachedIps = new List<string> { "127.0.0.1", "192.168.0.1" };

        mockCache.Setup(c => c.TryGetValueAsync<List<string>>(It.IsAny<string>())).ReturnsAsync((true, cachedIps));

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var ips = await service.GetUserIPsStream(1).ToListAsync();

        // Assert
        Assert.Equal(cachedIps, ips);
        mockCache.Verify(c => c.TryGetValueAsync<List<string>>(It.IsAny<string>()), Times.Once);
        mockRepo.Verify(
            r => r.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task GetUserIPs_Should_ReturnIPsFromCache_When_Cache_IsNullAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        mockCache.Setup(c => c.TryGetValueAsync<List<string>>(It.IsAny<string>())).ReturnsAsync((true, null));
        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var ips = await service.GetUserIPsStream(1).ToListAsync();

        // Assert
        Assert.Empty(ips);
        mockCache.Verify(c => c.TryGetValueAsync<List<string>>(It.IsAny<string>()), Times.Once);
        mockRepo.Verify(
            r => r.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task GetUserIPs_Should_ReturnIPsFromCache_When_NoAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        mockCache.Setup(c => c.TryGetValueAsync<List<string>>(It.IsAny<string>())).ReturnsAsync((false, null));
        mockRepo.Setup(c =>
                c.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, string>>(), It.IsAny<CancellationToken>()))
            .Returns(new List<string>().ToConfiguredCancelableAsyncEnumerable());

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var ips = await service.GetUserIPsStream(1).ToListAsync();

        // Assert
        Assert.Empty(ips);
        mockCache.Verify(c => c.TryGetValueAsync<List<string>>(It.IsAny<string>()), Times.Exactly(2));
        mockRepo.Verify(
            r => r.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Once);
    }

    #endregion

    #region FindUsersByIpPart

    [Fact]
    public async Task FindUsersByIpPart_Should_ReturnUsersFromCache_When_DB_IsAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        var dbIds = new List<long> { 1, 2 };

        mockCache.Setup(c => c.TryGetValueAsync<List<long>>(It.IsAny<string>())).ReturnsAsync((false, null));
        mockRepo.Setup(c =>
                c.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, long>>(), It.IsAny<CancellationToken>()))
            .Returns(dbIds.ToConfiguredCancelableAsyncEnumerable());

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);


        // Act
        var ids = await service.FindUsersByIpPartStream("127.0.").ToListAsync();

        // Assert
        Assert.Equal(dbIds, ids);
        mockCache.Verify(c => c.TryGetValueAsync<List<long>>(It.IsAny<string>()), Times.Exactly(2));
        mockRepo.Verify(
            r => r.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, long>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<long>>()), Times.Once);
    }

    [Fact]
    public async Task FindUsersByIpPart_Should_ReturnUsersFromCache_When_Cache_IsAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        var cachedIds = new List<long> { 1, 2 };

        mockCache.Setup(c => c.TryGetValueAsync<List<long>>(It.IsAny<string>())).ReturnsAsync((true, cachedIds));

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var ids = await service.FindUsersByIpPartStream("127.0.").ToListAsync();

        // Assert
        Assert.Equal(cachedIds, ids);
        mockCache.Verify(c => c.TryGetValueAsync<List<long>>(It.IsAny<string>()), Times.Once);
        mockRepo.Verify(
            r => r.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, long>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<long>>()), Times.Never);
    }

    [Fact]
    public async Task FindUsersByIpPart_Should_ReturnUsersFromCache_When_Cache_IsNullAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        mockCache.Setup(c => c.TryGetValueAsync<List<long>>(It.IsAny<string>())).ReturnsAsync((true, null));

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var ids = await service.FindUsersByIpPartStream("127.0.").ToListAsync();

        // Assert
        Assert.Empty(ids);
        mockCache.Verify(c => c.TryGetValueAsync<List<long>>(It.IsAny<string>()), Times.Once);
        mockRepo.Verify(
            r => r.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, long>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<long>>()), Times.Never);
    }

    [Fact]
    public async Task FindUsersByIpPart_Should_ReturnUsersFromCache_When_NoAvailable()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        var mockCache = new Mock<ICache>();

        var mockLogger = new Mock<ILogger<IPLogsService>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        mockCache.Setup(c => c.TryGetValueAsync<List<long>>(It.IsAny<string>())).ReturnsAsync((false, null));
        mockRepo.Setup(c =>
                c.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, long>>(), It.IsAny<CancellationToken>()))
            .Returns(new List<long>().ToConfiguredCancelableAsyncEnumerable());

        var service = new IPLogsService(mockRepo.Object, mockCache.Object, mockLoggerFactory.Object);

        // Act
        var ids = await service.FindUsersByIpPartStream("127.0.").ToListAsync();

        // Assert
        Assert.Empty(ids);
        mockCache.Verify(c => c.TryGetValueAsync<List<long>>(It.IsAny<string>()), Times.Exactly(2));
        mockRepo.Verify(
            r => r.AsAsyncEnumerableStream(It.IsAny<ISpecification<UserIP, long>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<long>>()), Times.Once);
    }

    #endregion
}