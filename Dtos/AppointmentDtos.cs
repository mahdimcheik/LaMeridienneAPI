using System.ComponentModel.DataAnnotations;
using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Dtos;

/// <summary>Client désigné par <c>customerId</c> <b>ou</b> par <c>customerName</c> (créé à la volée, R-08).</summary>
public sealed record AppointmentRequest
{
    [Required(ErrorMessage = "Saisissez le début du rendez-vous.")]
    public DateTimeOffset? Start { get; init; }

    [Required(ErrorMessage = "Saisissez la fin du rendez-vous.")]
    public DateTimeOffset? End { get; init; }

    public Guid? CustomerId { get; init; }

    [MaxLength(Customer.NameMaxLength, ErrorMessage = "Le nom du client fait au plus 100 caractères.")]
    public string? CustomerName { get; init; }

    [MaxLength(TireAppointment.PhoneMaxLength, ErrorMessage = "Le téléphone fait au plus 30 caractères.")]
    public string? Phone { get; init; }

    [MaxLength(TireAppointment.VehicleMaxLength, ErrorMessage = "Le véhicule fait au plus 100 caractères.")]
    public string? Vehicle { get; init; }

    [MaxLength(TireAppointment.ServiceMaxLength, ErrorMessage = "La prestation fait au plus 200 caractères.")]
    public string? Service { get; init; }

    [MaxLength(TireAppointment.NoteMaxLength, ErrorMessage = "La note fait au plus 1 000 caractères.")]
    public string? Note { get; init; }
}

public sealed record AppointmentResponse(
    Guid Id,
    DateTimeOffset Start,
    DateTimeOffset End,
    CustomerResponse Customer,
    string? Phone,
    string? Vehicle,
    string? Service,
    string? Note,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    string? UpdatedBy,
    DateTimeOffset? UpdatedAt);

/// <summary>Rendez-vous qui partage une partie du créneau ; des bornes qui se touchent ne comptent pas (R-13).</summary>
public sealed record AppointmentOverlap(Guid Id, DateTimeOffset Start, DateTimeOffset End, string CustomerName);

/// <summary>Réponse d'une création ou d'une modification : le rendez-vous est enregistré même en cas de chevauchement.</summary>
public sealed record AppointmentSaveResponse(AppointmentResponse Appointment, IReadOnlyList<AppointmentOverlap> Overlaps);
