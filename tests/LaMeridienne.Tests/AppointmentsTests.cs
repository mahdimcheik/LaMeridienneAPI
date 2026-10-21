using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

/// <summary>Agenda des pneus (E07) : R-12 (durée, pas de 15 minutes) et R-13 (chevauchements signalés).</summary>
[Collection(DatabaseCollection.Name)]
public sealed class AppointmentsTests(PostgresFixture db)
{
    private const string Week = "/api/appointments?from=2026-10-19T00:00:00%2B02:00&to=2026-10-26T00:00:00%2B01:00";
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-21T08:00:00Z"));

    private Task<ApiFactory> FactoryAsync() =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("appointments_" + Guid.NewGuid().ToString("N")), time: _time);

    private static object Slot(string start, string end, string customerName = "Dupont") =>
        new { start, end, customerName, vehicle = (string?)"Clio AB-123-CD", service = (string?)"4 pneus été" };

    [Fact]
    public async Task US_E07_02_creer_un_rendez_vous_cree_le_client_et_rend_l_instant_en_UTC()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var saved = await client.AddAppointmentAsync(new
        {
            start = "2026-10-22T10:00:00+02:00", end = "2026-10-22T11:00:00+02:00", customerName = " Dupont ",
            phone = " 06 12 ", vehicle = "Clio AB-123-CD", service = "4 pneus été + équilibrage", note = "  ",
        });

        var a = saved.Appointment;
        Assert.Empty(saved.Overlaps);
        Assert.Equal(DateTimeOffset.Parse("2026-10-22T08:00:00Z"), a.Start);
        Assert.Equal(TimeSpan.Zero, a.Start.Offset);
        Assert.Equal(("Dupont", "06 12", "Clio AB-123-CD", "4 pneus été + équilibrage", (string?)null),
            (a.Customer.Name, a.Phone, a.Vehicle, a.Service, a.Note));
        Assert.Equal(ApiFactory.Accounts[0].DisplayName, a.CreatedBy);

        var json = await client.GetStringAsync(Week);
        Assert.Contains("\"start\":\"2026-10-22T08:00:00Z\"", json);

        var byId = await client.AddAppointmentAsync(new
        {
            start = "2026-10-23T08:00:00Z", end = "2026-10-23T08:15:00Z", customerId = a.Customer.Id,
        });
        Assert.Equal(a.Customer.Id, byId.Appointment.Customer.Id);
    }

    [Fact]
    public async Task US_E07_01_la_periode_rend_les_rendez_vous_qui_la_touchent_par_debut()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        await client.AddAppointmentAsync(Slot("2026-10-23T08:00:00Z", "2026-10-23T09:00:00Z", "Martin"));
        await client.AddAppointmentAsync(Slot("2026-10-18T21:45:00Z", "2026-10-18T22:30:00Z", "Avant-veille"));
        await client.AddAppointmentAsync(Slot("2026-10-20T08:00:00Z", "2026-10-20T09:00:00Z", "Durand"));
        await client.AddAppointmentAsync(Slot("2026-10-26T09:00:00Z", "2026-10-26T10:00:00Z", "Après"));

        var week = await client.GetFromJsonAsync<List<AppointmentResponse>>(Week);

        Assert.Equal(["Avant-veille", "Durand", "Martin"], week!.Select(a => a.Customer.Name));
    }

    [Theory]
    [InlineData("from=2026-10-01T00:00:00Z&to=2026-12-02T00:00:01Z", "to")]
    [InlineData("to=2026-10-02T00:00:00Z", "from")]
    [InlineData("from=2026-10-01T00:00:00Z", "to")]
    [InlineData("from=2026-10-02T00:00:00Z&to=2026-10-02T00:00:00Z", "to")]
    public async Task US_E07_01_une_periode_invalide_ou_de_plus_de_62_jours_repond_400(string query, string field)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.GetAsync("/api/appointments?" + query);

        Assert.Equal([field], (await response.ValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task US_E07_01_une_periode_de_62_jours_exactement_est_acceptee()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.GetAsync("/api/appointments?from=2026-10-01T00:00:00Z&to=2026-12-02T00:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("2026-10-22T10:00:00Z", "2026-10-22T10:00:00Z", "end")]
    [InlineData("2026-10-22T10:00:00Z", "2026-10-22T09:00:00Z", "end")]
    [InlineData("2026-10-22T10:00:00Z", "2026-10-22T10:10:00Z", "end")]
    [InlineData("2026-10-22T06:00:00Z", "2026-10-22T14:15:00Z", "end")]
    [InlineData("2026-10-22T10:07:00Z", "2026-10-22T11:07:00Z", "start")]
    [InlineData("2026-10-22T10:00:00Z", "2026-10-22T11:00:30Z", "end")]
    [InlineData(null, "2026-10-22T11:00:00Z", "start")]
    public async Task R_12_duree_et_pas_de_15_minutes_sont_controles(string? start, string end, string field)
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var response = await client.PostAppointmentAsync(new { start, end, customerName = "Dupont" });

        Assert.Equal([field], (await response.ValidationProblemAsync()).Errors.Keys);
        Assert.Empty((await client.GetFromJsonAsync<List<AppointmentResponse>>(Week))!);
    }

    [Fact]
    public async Task R_12_huit_heures_et_un_rendez_vous_passe_sont_acceptes_le_client_est_obligatoire()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        await client.AddAppointmentAsync(Slot("2026-10-22T06:00:00Z", "2026-10-22T14:00:00Z"));
        await client.AddAppointmentAsync(Slot("2026-09-01T08:00:00Z", "2026-09-01T08:15:00Z"));
        var noCustomer = await client.PostAppointmentAsync(new { start = "2026-10-22T06:00:00Z", end = "2026-10-22T07:00:00Z" });

        Assert.Equal(["customerName"], (await noCustomer.ValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task R_13_un_chevauchement_est_accepte_et_nomme_le_rendez_vous_en_conflit()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var martin = (await client.AddAppointmentAsync(Slot("2026-10-22T08:00:00Z", "2026-10-22T09:00:00Z", "Martin"))).Appointment;
        var before = await client.AddAppointmentAsync(Slot("2026-10-22T07:00:00Z", "2026-10-22T08:00:00Z", "Avant"));

        var second = await client.AddAppointmentAsync(Slot("2026-10-22T08:30:00Z", "2026-10-22T09:30:00Z", "Dupont"));

        Assert.Empty(before.Overlaps);
        var overlap = Assert.Single(second.Overlaps);
        Assert.Equal((martin.Id, "Martin", martin.Start, martin.End), (overlap.Id, overlap.CustomerName, overlap.Start, overlap.End));
        Assert.Equal(3, (await client.GetFromJsonAsync<List<AppointmentResponse>>(Week))!.Count);
    }

    [Fact]
    public async Task US_E07_04_deplacer_un_rendez_vous_le_liste_au_nouveau_creneau_sans_se_signaler_lui_meme()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var (paul, _) = await factory.LoginAsync(1);
        var a = (await client.AddAppointmentAsync(Slot("2026-10-22T08:00:00Z", "2026-10-22T09:00:00Z"))).Appointment;
        var other = (await client.AddAppointmentAsync(Slot("2026-10-23T10:00:00Z", "2026-10-23T11:00:00Z", "Martin"))).Appointment;

        _time.Advance(TimeSpan.FromMinutes(5));
        var moved = await paul.PutAsJsonAsync($"/api/appointments/{a.Id}",
            Slot("2026-10-22T08:15:00Z", "2026-10-22T09:15:00Z"));
        var onto = await paul.PutAsJsonAsync($"/api/appointments/{a.Id}",
            Slot("2026-10-23T10:30:00Z", "2026-10-23T11:30:00Z"));

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var first = (await moved.Content.ReadFromJsonAsync<AppointmentSaveResponse>())!;
        Assert.Empty(first.Overlaps);
        Assert.Equal(DateTimeOffset.Parse("2026-10-22T08:15:00Z"), first.Appointment.Start);
        Assert.Equal(ApiFactory.Accounts[1].DisplayName, first.Appointment.UpdatedBy);
        Assert.Equal(_time.GetUtcNow(), first.Appointment.UpdatedAt);
        var second = (await onto.Content.ReadFromJsonAsync<AppointmentSaveResponse>())!;
        Assert.Equal([other.Id], second.Overlaps.Select(o => o.Id));

        var week = await client.GetFromJsonAsync<List<AppointmentResponse>>(Week);
        Assert.Equal([a.Id, other.Id], week!.Select(x => x.Id).Order());
        Assert.Equal(DateTimeOffset.Parse("2026-10-23T10:30:00Z"), week.Single(x => x.Id == a.Id).Start);
    }

    [Fact]
    public async Task US_E07_05_un_rendez_vous_supprime_disparait_et_repond_404()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var a = (await client.AddAppointmentAsync(Slot("2026-10-22T08:00:00Z", "2026-10-22T09:00:00Z"))).Appointment;

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/appointments/{a.Id}")).StatusCode);

        Assert.Empty((await client.GetFromJsonAsync<List<AppointmentResponse>>(Week))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/appointments/{a.Id}")).StatusCode);
        var update = await client.PutAsJsonAsync($"/api/appointments/{a.Id}", Slot("2026-10-22T08:00:00Z", "2026-10-22T09:00:00Z"));
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal("application/problem+json", update.Content.Headers.ContentType?.MediaType);
        var overlapping = await client.AddAppointmentAsync(Slot("2026-10-22T08:00:00Z", "2026-10-22T09:00:00Z"));
        Assert.Empty(overlapping.Overlaps);

        await using var scope = factory.Services.CreateAsyncScope();
        var raw = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .TireAppointments.IgnoreQueryFilters().SingleAsync(x => x.Id == a.Id);
        Assert.Equal(_time.GetUtcNow(), raw.DeletedAt);
    }

    [Fact]
    public async Task US_E08_02_la_fusion_deplace_aussi_les_rendez_vous_meme_supprimes()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();
        var a = (await client.AddAppointmentAsync(Slot("2026-10-22T08:00:00Z", "2026-10-22T09:00:00Z", "Dupont"))).Appointment.Customer;
        var kept = (await client.AddAppointmentAsync(Slot("2026-10-23T08:00:00Z", "2026-10-23T09:00:00Z", "Dupond"))).Appointment;
        var deleted = (await client.AddAppointmentAsync(new { start = "2026-10-24T08:00:00Z", end = "2026-10-24T09:00:00Z", customerId = kept.Customer.Id })).Appointment;
        await client.DeleteAsync($"/api/appointments/{deleted.Id}");

        var merge = await client.PostAsJsonAsync($"/api/customers/{a.Id}/merge", new { sourceId = kept.Customer.Id });

        Assert.Equal(HttpStatusCode.OK, merge.StatusCode);
        var week = await client.GetFromJsonAsync<List<AppointmentResponse>>(Week);
        Assert.All(week!, x => Assert.Equal(("Dupont", a.Id), (x.Customer.Name, x.Customer.Id)));
        await using var scope = factory.Services.CreateAsyncScope();
        var raw = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .TireAppointments.IgnoreQueryFilters().SingleAsync(x => x.Id == deleted.Id);
        Assert.Equal(a.Id, raw.CustomerId);
    }

    [Fact]
    public async Task L_agenda_exige_un_jeton()
    {
        await using var factory = await FactoryAsync();

        var response = await factory.RawClient().GetAsync(Week);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
