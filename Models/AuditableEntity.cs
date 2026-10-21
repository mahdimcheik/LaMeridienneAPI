namespace LaMeridienneAPI.Models;

/// <summary>Colonnes d'audit posées par <see cref="Data.AuditInterceptor"/>, jamais par l'appelant.</summary>
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
}
