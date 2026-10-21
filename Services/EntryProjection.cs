using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using LaMeridienneAPI.Models;

namespace LaMeridienneAPI.Services;

public static class EntryProjection
{
    /// <summary>Projection SQL d'une ligne, avec le nom affiché de son auteur et du dernier modificateur.</summary>
    public static IQueryable<EntryResponse> ToResponses<T>(this IQueryable<T> entries, AppDbContext db) where T : CustomerEntry =>
        entries.Select(e => new EntryResponse(
            e.Id,
            e.Sheet.Date,
            new CustomerRef(e.Customer.Id, e.Customer.Name),
            e.Amount,
            e.Comment,
            db.Users.Where(u => u.Id == e.CreatedById).Select(u => u.DisplayName).FirstOrDefault(),
            e.CreatedAt,
            db.Users.Where(u => u.Id == e.UpdatedById).Select(u => u.DisplayName).FirstOrDefault(),
            e.UpdatedAt));
}
