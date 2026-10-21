using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaMeridienneAPI.Data.Configurations;

public sealed class DailySheetConfiguration : IEntityTypeConfiguration<DailySheet>
{
    public void Configure(EntityTypeBuilder<DailySheet> b)
    {
        b.Property(s => s.Id).ValueGeneratedNever();
        b.HasIndex(s => s.Date).IsUnique();
        b.Property(s => s.Notes).HasMaxLength(DailySheet.NotesMaxLength);
        b.Property(s => s.NotesVersion).IsConcurrencyToken();
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Name).HasMaxLength(Customer.NameMaxLength);
        b.Property(c => c.NormalizedName).HasMaxLength(Customer.NameMaxLength);
        b.Property(c => c.Phone).HasMaxLength(30);
        b.HasIndex(c => c.NormalizedName).IsUnique().HasFilter("\"MergedIntoId\" IS NULL");
        b.HasOne<Customer>().WithMany().HasForeignKey(c => c.MergedIntoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(c => c.NormalizedName, "IX_Customers_NormalizedName_Trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}

public abstract class CustomerEntryConfiguration<T> : IEntityTypeConfiguration<T> where T : CustomerEntry
{
    public void Configure(EntityTypeBuilder<T> b)
    {
        b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.Amount).HasPrecision(10, 2);
        b.Property(e => e.Comment).HasMaxLength(CustomerEntry.CommentMaxLength);
        b.HasOne(e => e.Sheet).WithMany().HasForeignKey(e => e.SheetId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint($"CK_{typeof(T).Name}_Amount",
            $"\"Amount\" > 0 AND \"Amount\" <= {CustomerEntry.MaxAmount}"));
    }
}

public sealed class CreditEntryConfiguration : CustomerEntryConfiguration<CreditEntry>;

public sealed class RefundEntryConfiguration : CustomerEntryConfiguration<RefundEntry>;

public sealed class SheetLayoutConfiguration : IEntityTypeConfiguration<SheetLayout>
{
    public void Configure(EntityTypeBuilder<SheetLayout> b)
    {
        b.HasKey(l => l.UserId);
        b.HasOne<User>().WithOne().HasForeignKey<SheetLayout>(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
