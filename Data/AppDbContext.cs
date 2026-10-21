using System.Linq.Expressions;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DailySheet> DailySheets => Set<DailySheet>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CreditEntry> CreditEntries => Set<CreditEntry>();
    public DbSet<RefundEntry> RefundEntries => Set<RefundEntry>();
    public DbSet<SheetLayout> SheetLayouts => Set<SheetLayout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null)
                continue;

            var e = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(e, nameof(ISoftDeletable.DeletedAt)),
                    Expression.Constant(null, typeof(DateTimeOffset?))),
                e);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
