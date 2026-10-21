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

    public static async Task<ValidationProblemDetails> ValidationProblemAsync(this HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
    }
}
