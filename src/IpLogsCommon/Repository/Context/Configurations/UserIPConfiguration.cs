using IpLogsCommon.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IpLogsCommon.Repository.Context.Configurations;

public class UserIPConfiguration : IEntityTypeConfiguration<UserIP>
{
    public void Configure(EntityTypeBuilder<UserIP> builder)
    {
        builder.HasKey(ui => ui.Id);
        builder.Property(ui => ui.UserId).IsRequired();
        builder.Property(ui => ui.IPAddress)
            .HasColumnType("VARCHAR(45)")
            .IsRequired();
        builder.Property(ui => ui.ConnectionTime).IsRequired();

        builder.HasOne(ui => ui.User)
            .WithMany(u => u.UserIPs)
            .HasForeignKey(ui => ui.UserId);
    }
}