using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaMeridienneAPI.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.Id).ValueGeneratedNever();
        b.Property(u => u.UserName).HasMaxLength(64);
        b.Property(u => u.DisplayName).HasMaxLength(64);
        b.Property(u => u.PasswordHash).HasMaxLength(256);
        b.HasIndex(u => u.UserName).IsUnique();
    }
}
