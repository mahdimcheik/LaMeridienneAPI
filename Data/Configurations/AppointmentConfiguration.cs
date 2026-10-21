using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaMeridienneAPI.Data.Configurations;

public sealed class TireAppointmentConfiguration : IEntityTypeConfiguration<TireAppointment>
{
    public void Configure(EntityTypeBuilder<TireAppointment> b)
    {
        b.Property(a => a.Id).ValueGeneratedNever();
        b.Property(a => a.Phone).HasMaxLength(TireAppointment.PhoneMaxLength);
        b.Property(a => a.Vehicle).HasMaxLength(TireAppointment.VehicleMaxLength);
        b.Property(a => a.Service).HasMaxLength(TireAppointment.ServiceMaxLength);
        b.Property(a => a.Note).HasMaxLength(TireAppointment.NoteMaxLength);
        b.HasOne(a => a.Customer).WithMany().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(a => a.Start);
        b.ToTable(t => t.HasCheckConstraint("CK_TireAppointments_End", "\"End\" > \"Start\""));
    }
}
