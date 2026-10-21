using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Infrastructure;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaMeridienneAPI.Services;

/// <summary>Agenda des pneus (E07) : durée et pas de 15 minutes (R-12), chevauchements signalés sans refus (R-13).</summary>
public sealed class AppointmentService(AppDbContext db, CustomerService customers)
{
    public static readonly TimeSpan MaxPeriod = TimeSpan.FromDays(62);

    /// <summary>Rendez-vous qui touchent la période <c>[from, to)</c>, par début.</summary>
    public async Task<IReadOnlyList<AppointmentResponse>> ListAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        if (from is null)
            throw new FieldValidationException("from", "Saisissez le début de la période.");
        if (to is null)
            throw new FieldValidationException("to", "Saisissez la fin de la période.");
        if (to <= from)
            throw new FieldValidationException("to", "La fin de la période doit être postérieure à son début.");
        if (to - from > MaxPeriod)
            throw new FieldValidationException("to", "La période demandée dépasse 62 jours.");

        var start = from.Value.ToUniversalTime();
        var end = to.Value.ToUniversalTime();
        return await Project(db.TireAppointments.Where(a => a.Start < end && a.End > start)
                .OrderBy(a => a.Start).ThenBy(a => a.CreatedAt))
            .ToListAsync(ct);
    }

    /// <summary>Un rendez-vous dans le passé est accepté (saisie a posteriori).</summary>
    public async Task<AppointmentSaveResponse> CreateAsync(AppointmentRequest request, CancellationToken ct)
    {
        var (start, end) = Slot(request);
        var appointment = new TireAppointment
        {
            CustomerId = (await customers.ResolveAsync(request.CustomerId, request.CustomerName, ct)).Id,
        };
        Apply(appointment, start, end, request);
        db.Add(appointment);
        await db.SaveChangesAsync(ct);
        return await SavedAsync(appointment, ct);
    }

    /// <summary>Dernier enregistrement gagnant (R-11) ; un rendez-vous supprimé entre-temps répond <c>404</c>.</summary>
    public async Task<AppointmentSaveResponse> UpdateAsync(Guid id, AppointmentRequest request, CancellationToken ct)
    {
        var appointment = await FindAsync(id, ct);
        var (start, end) = Slot(request);
        appointment.CustomerId = (await customers.ResolveAsync(request.CustomerId, request.CustomerName, ct)).Id;
        Apply(appointment, start, end, request);
        await db.SaveChangesAsync(ct);
        return await SavedAsync(appointment, ct);
    }

    /// <summary>Suppression logique (R-06), posée par l'intercepteur d'audit.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        db.Remove(await FindAsync(id, ct));
        await db.SaveChangesAsync(ct);
    }

    private async Task<TireAppointment> FindAsync(Guid id, CancellationToken ct) =>
        await db.TireAppointments.SingleOrDefaultAsync(a => a.Id == id, ct)
        ?? throw new NotFoundException("Ce rendez-vous n'existe plus : rechargez l'agenda.");

    private async Task<AppointmentSaveResponse> SavedAsync(TireAppointment appointment, CancellationToken ct)
    {
        var response = await Project(db.TireAppointments.Where(a => a.Id == appointment.Id)).SingleAsync(ct);
        var overlaps = await db.TireAppointments.AsNoTracking()
            .Where(a => a.Id != appointment.Id && a.Start < appointment.End && a.End > appointment.Start)
            .OrderBy(a => a.Start).ThenBy(a => a.CreatedAt)
            .Select(a => new AppointmentOverlap(a.Id, a.Start, a.End, a.Customer.Name))
            .ToListAsync(ct);
        return new AppointmentSaveResponse(response, overlaps);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) Slot(AppointmentRequest request)
    {
        var start = request.Start!.Value.ToUniversalTime();
        var end = request.End!.Value.ToUniversalTime();
        if (end <= start)
            throw new FieldValidationException("end", "La fin doit être postérieure au début.");
        if (end - start < TireAppointment.MinDuration)
            throw new FieldValidationException("end", "Un rendez-vous dure au moins 15 minutes.");
        if (end - start > TireAppointment.MaxDuration)
            throw new FieldValidationException("end", "Un rendez-vous dure au plus 8 heures.");
        if (start.Ticks % TireAppointment.Step.Ticks != 0)
            throw new FieldValidationException("start", "Le début tombe sur un pas de 15 minutes.");
        if (end.Ticks % TireAppointment.Step.Ticks != 0)
            throw new FieldValidationException("end", "La fin tombe sur un pas de 15 minutes.");
        return (start, end);
    }

    private static void Apply(TireAppointment appointment, DateTimeOffset start, DateTimeOffset end, AppointmentRequest request)
    {
        appointment.Start = start;
        appointment.End = end;
        appointment.Phone = Clean(request.Phone);
        appointment.Vehicle = Clean(request.Vehicle);
        appointment.Service = Clean(request.Service);
        appointment.Note = Clean(request.Note);
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private IQueryable<AppointmentResponse> Project(IQueryable<TireAppointment> appointments) =>
        appointments.Select(a => new AppointmentResponse(
            a.Id,
            a.Start,
            a.End,
            new CustomerResponse(a.Customer.Id, a.Customer.Name, a.Customer.Phone),
            a.Phone,
            a.Vehicle,
            a.Service,
            a.Note,
            db.Users.Where(u => u.Id == a.CreatedById).Select(u => u.DisplayName).FirstOrDefault(),
            a.CreatedAt,
            db.Users.Where(u => u.Id == a.UpdatedById).Select(u => u.DisplayName).FirstOrDefault(),
            a.UpdatedAt));
}
