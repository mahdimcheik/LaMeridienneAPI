using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Dtos;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

/// <summary>La journée type du parcours utilisateurs (00-vision/04), rejouée de bout en bout.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class JourneeTypeTests(PostgresFixture db)
{
    [Fact]
    public async Task Une_journee_au_comptoir_a_deux_postes()
    {
        // Ancrée sur la date réelle : le bocal à cookies du client juge l'expiration à l'horloge réelle.
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var time = new FakeTimeProvider(new DateTimeOffset(day.ToDateTime(new TimeOnly(5, 30)), TimeSpan.Zero)); // 6 h 30 ou 7 h 30 à Paris
        await using var factory = await ApiFactory.CreateAsync(
            db.ConnectionStringFor("journee_" + Guid.NewGuid().ToString("N")), time: time);
        var (marie, _) = await factory.LoginAsync(0);
        var (paul, _) = await factory.LoginAsync(1);

        // Les deux postes ouvrent la feuille du jour en même temps.
        var opened = await Task.WhenAll(
            marie.GetFromJsonAsync<SheetResponse>("/api/sheets/today"),
            paul.GetFromJsonAsync<SheetResponse>("/api/sheets/today"));
        Assert.All(opened, s => Assert.Equal((day, true), (s!.Date, s.Exists)));
        var today = day.ToString("yyyy-MM-dd");

        async Task Later(TimeSpan delay)
        {
            time.Advance(delay);
            await Task.WhenAll(marie.RenewAsync(), paul.RenewAsync());
        }

        // Matinée : un crédit, un remboursement, une note.
        await Later(TimeSpan.FromHours(1));
        var credit = await marie.AddEntryAsync("credits", today, new { customerName = "Dupont", amount = 12.5, comment = "pain + journal" });
        var refund = await paul.AddEntryAsync("refunds", today, new { customerName = "Martin", amount = 2, comment = "erreur de rendu" });
        var notes = await (await marie.PutAsJsonAsync($"/api/sheets/{today}/notes",
            new { notes = "Livraison Sud Ouest en retard", version = 0 })).Content.ReadFromJsonAsync<NotesResponse>();

        // Paul retrouve Dupont par autocomplétion et lui note un second crédit.
        var proposals = await paul.GetFromJsonAsync<List<CustomerResponse>>("/api/customers?search=dup");
        await paul.AddEntryAsync("credits", today, new { customerId = proposals!.Single().Id, amount = 3.2 });

        // Paul complète la note sur la version qu'il a lue ; Marie, partie d'une version périmée, est prévenue.
        var paulNotes = await paul.PutAsJsonAsync($"/api/sheets/{today}/notes",
            new { notes = notes!.Notes + "\nRappeler M. Martin", version = notes.NotesVersion });
        Assert.Equal(HttpStatusCode.OK, paulNotes.StatusCode);
        var stale = await marie.PutAsJsonAsync($"/api/sheets/{today}/notes", new { notes = "écrasé ?", version = notes.NotesVersion });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        // Après-midi : Marie corrige le remboursement de Paul et supprime le crédit réglé.
        await Later(TimeSpan.FromHours(5));
        await marie.PutAsJsonAsync($"/api/refunds/{refund.Id}", new { customerName = "Martin", amount = 2.5, comment = "erreur de rendu" });
        await marie.DeleteAsync($"/api/credits/{credit.Id}");

        // Fin de journée : chaque poste voit les saisies de tous, avec leurs auteurs.
        var sheet = await paul.SheetAsync(today);
        var remaining = Assert.Single(sheet.Credits);
        Assert.Equal(("Dupont", 3.2m, "Paul"), (remaining.Customer.Name, remaining.Amount, remaining.CreatedBy));
        var corrected = Assert.Single(sheet.Refunds);
        Assert.Equal((2.5m, "Paul", "Marie"), (corrected.Amount, corrected.CreatedBy, corrected.UpdatedBy));
        Assert.Equal(("Livraison Sud Ouest en retard\nRappeler M. Martin", 2), (sheet.Notes, sheet.NotesVersion));

        // Le lendemain, la feuille de la veille reste consultable ; la nouvelle est vierge.
        await Later(TimeSpan.FromHours(12));
        var tomorrow = await marie.GetFromJsonAsync<SheetResponse>("/api/sheets/today");
        Assert.Equal((day.AddDays(1), 0), (tomorrow!.Date, tomorrow.Credits.Count));
        Assert.Single((await marie.SheetAsync(today)).Credits);
    }
}
