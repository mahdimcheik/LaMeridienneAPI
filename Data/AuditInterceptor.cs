using LaMeridienneAPI.Auth;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LaMeridienneAPI.Data;

public sealed class AuditInterceptor(TimeProvider time, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;

        var now = time.GetUtcNow();
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable deletable })
            {
                entry.State = EntityState.Modified;
                deletable.DeletedAt = now;
            }

            if (entry.Entity is not AuditableEntity audited) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    audited.CreatedAt = now;
                    audited.CreatedById = userId;
                    break;
                case EntityState.Modified:
                    audited.UpdatedAt = now;
                    audited.UpdatedById = userId;
                    entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(AuditableEntity.CreatedById)).IsModified = false;
                    break;
            }
        }
    }
}
