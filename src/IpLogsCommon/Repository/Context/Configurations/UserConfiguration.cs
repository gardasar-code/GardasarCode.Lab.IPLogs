using IpLogsCommon.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IpLogsCommon.Repository.Context.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(u => u.IPAddress)
            .HasColumnType("VARCHAR(45)")
            .IsRequired();
        builder.Property(u => u.LastConnectionTime).IsRequired();

        builder.HasMany(u => u.UserIPs)
            .WithOne(ui => ui.User)
            .HasForeignKey(ui => ui.UserId);
    }
}