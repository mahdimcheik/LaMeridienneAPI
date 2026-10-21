using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaMeridienneAPI.Data.Configurations;

public sealed class NewspaperConfiguration : IEntityTypeConfiguration<Newspaper>
{
    public void Configure(EntityTypeBuilder<Newspaper> b)
    {
        b.Property(n => n.Id).ValueGeneratedNever();
        b.Property(n => n.Name).HasMaxLength(Newspaper.NameMaxLength);
        b.Property(n => n.NormalizedName).HasMaxLength(Newspaper.NameMaxLength);
        b.HasIndex(n => n.NormalizedName).IsUnique();
        b.HasData(
            new Newspaper { Id = Newspaper.SudOuestId, Name = "Sud Ouest", NormalizedName = "sud ouest", IsActive = true, SortOrder = 1 },
            new Newspaper { Id = Newspaper.LeRepublicainId, Name = "Le Républicain", NormalizedName = "le republicain", IsActive = true, SortOrder = 2 });
    }
}

public sealed class NewspaperSaleConfiguration : IEntityTypeConfiguration<NewspaperSale>
{
    public void Configure(EntityTypeBuilder<NewspaperSale> b)
    {
        b.HasKey(s => new { s.SheetId, s.NewspaperId });
        b.HasOne<DailySheet>().WithMany().HasForeignKey(s => s.SheetId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Newspaper>().WithMany().HasForeignKey(s => s.NewspaperId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_NewspaperSale_Quantity",
            $"\"Quantity\" >= 0 AND \"Quantity\" <= {NewspaperSale.MaxQuantity}"));
    }
}
