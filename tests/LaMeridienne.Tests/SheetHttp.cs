using System.Net;
using System.Net.Http.Json;
using LaMeridienneAPI.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace LaMeridienne.Tests;

/// <summary>Appels de la feuille du jour, partagés par les tests des lots 2 et suivants.</summary>
public static class SheetHttp
{
    public static async Task<SheetResponse> SheetAsync(this HttpClient client, string date)
    {
        var response = await client.GetAsync($"/api/sheets/{date}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SheetResponse>())!;
    }

    public static Task<HttpResponseMessage> PostEntryAsync(this HttpClient client, string kind, string date, object body) =>
        client.PostAsJsonAsync($"/api/sheets/{date}/{kind}", body);

    public static async Task<EntryResponse> AddEntryAsync(this HttpClient client, string kind, string date, object body)
    {
        var response = await client.PostEntryAsync(kind, date, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EntryResponse>())!;
    }

    public static Task<HttpResponseMessage> IncrementAsync(this HttpClient client, string date, Guid newspaperId, int delta) =>
        client.PostAsJsonAsync($"/api/sheets/{date}/newspapers/{newspaperId}/increment", new { delta });

    public static Task<HttpResponseMessage> SetQuantityAsync(this HttpClient client, string date, Guid newspaperId, object quantity) =>
        client.PutAsJsonAsync($"/api/sheets/{date}/newspapers/{newspaperId}", new { quantity });

    public static async Task<NewspaperResponse> AddNewspaperAsync(this HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/newspapers", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<NewspaperResponse>())!;
    }

    public static async Task<ValidationProblemDetails> ValidationProblemAsync(this HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
    }
}
