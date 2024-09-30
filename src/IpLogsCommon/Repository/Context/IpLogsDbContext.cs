using IpLogsCommon.Repository.Context.Configurations;
using IpLogsCommon.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace IpLogsCommon.Repository.Context;

public class IpLogsDbContext(DbContextOptions<IpLogsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; init; }
    public DbSet<UserIP> UserIPs { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserIPConfiguration());
    }
}