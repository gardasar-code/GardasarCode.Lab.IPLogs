using IpLogsCommon.Repository;
using IpLogsCommon.Repository.Context;
using IpLogsCommon.Repository.Entities;
using IpLogsCommon.Repository.Interfaces;
using IpLogsCommon.Repository.Specifications;
using Microsoft.EntityFrameworkCore;

namespace IpLogsTests;

public class UnitTestUserSpecification
{
    [Fact]
    public async Task GetUserById_User_IsAvailable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IpLogsDbContext>()
            .UseInMemoryDatabase("DBGetUserById")
            .Options;

        const bool asNoTracking = true;
        const long userId = 1L;

        await using var context = new IpLogsDbContext(options);
        using IRepository<DbContext> repo = new RepositoryBase<IpLogsDbContext>(context);

        context.Users.Add(new User { Id = 1L });
        context.Users.Add(new User { Id = 2L });
        await context.SaveChangesAsync();

        // Act
        var spec = new UserSpecification.GetUserById(userId, asNoTracking);
        var result = await repo.FirstOrDefaultAsync(spec);

        // Assert
        Assert.True(spec.AsNoTracking);
        Assert.Single(spec.Criterias);
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task GetUserIpsById_UserIps_IsAvailable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IpLogsDbContext>()
            .UseInMemoryDatabase("DBGetUserIpsById")
            .Options;

        const bool asNoTracking = true;
        const long userId = 1L;

        await using var context = new IpLogsDbContext(options);
        using IRepository<DbContext> repo = new RepositoryBase<IpLogsDbContext>(context);

        context.UserIPs.Add(new UserIP { UserId = 1L, IPAddress = "127.0.0.1", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 1L, IPAddress = "127.0.0.1", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 1L, IPAddress = "127.0.0.2", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 1L, IPAddress = "127.0.0.3", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 2L, IPAddress = "127.0.0.1", ConnectionTime = DateTime.Now });
        await context.SaveChangesAsync();

        // Act
        var spec = new UserSpecification.GetUserIpsById(userId, asNoTracking);

        var ips = new List<string>();
        await foreach (var ip in repo.AsAsyncEnumerableStream(spec)) ips.Add(ip);

        // Assert
        Assert.True(spec.AsNoTracking);
        Assert.True(spec.Distinct);
        Assert.Single(spec.Criterias);

        Assert.NotNull(ips);
        Assert.Equal(3, ips.Count);
    }

    [Fact]
    public async Task FindUsersByIpPart_Users_IsAvailable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IpLogsDbContext>()
            .UseInMemoryDatabase("DBFindUsersByIpPart")
            .Options;

        const bool asNoTracking = true;
        const string ipPart = "127.0.";

        List<long> waitedIds = [1, 2, 3];

        await using var context = new IpLogsDbContext(options);
        using IRepository<DbContext> repo = new RepositoryBase<IpLogsDbContext>(context);

        context.UserIPs.Add(new UserIP { UserId = 1L, IPAddress = "127.0.0.1", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 1L, IPAddress = "127.0.0.1", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 1L, IPAddress = "127.0.0.2", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 1L, IPAddress = "127.0.0.3", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 2L, IPAddress = "127.0.0.1", ConnectionTime = DateTime.Now });
        context.UserIPs.Add(new UserIP { UserId = 3L, IPAddress = "127.0.0.1", ConnectionTime = DateTime.Now });
        await context.SaveChangesAsync();

        // Act
        var spec = new UserSpecification.FindUsersByIpPart(ipPart, asNoTracking);

        var ids = new List<long>();
        await foreach (var ip in repo.AsAsyncEnumerableStream(spec)) ids.Add(ip);

        // Assert
        Assert.True(spec.AsNoTracking);
        Assert.True(spec.Distinct);
        Assert.Single(spec.Criterias);

        Assert.NotNull(ids);
        Assert.Equal(waitedIds, ids);
        Assert.Equal(3, ids.Count);
    }

    [Fact]
    public async Task Repository_Add()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<IpLogsDbContext>()
            .UseInMemoryDatabase("DBRepository")
            .Options;

        var user = new User { Id = 10, IPAddress = "127.0.0.1", LastConnectionTime = DateTime.Now };

        await using var context = new IpLogsDbContext(options);
        using IRepository<DbContext> repo = new RepositoryBase<IpLogsDbContext>(context);

        // Act
        _ = await repo.AddAsync(user);
        await context.SaveChangesAsync();
        var spec = new UserSpecification.GetUserById(user.Id, true);
        var result = await repo.FirstOrDefaultAsync(spec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }
}