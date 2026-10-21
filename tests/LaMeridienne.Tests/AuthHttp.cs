using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LaMeridienne.Tests;

/// <summary>Appels d'authentification avec un cookie de renouvellement manipulé à la main.</summary>
public static class AuthHttp
{
    public const string Cookie = "lm_refresh";

    /// <summary>Client sans bocal à cookies : chaque test choisit le jeton qu'il présente.</summary>
    public static HttpClient RawClient(this ApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost"),
        });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string userName, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { userName, password });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, int account = 0) =>
        client.LoginAsync(ApiFactory.Accounts[account].UserName, ApiFactory.Accounts[account].Password);

    public static Task<HttpResponseMessage> PostWithCookieAsync(this HttpClient client, string path, string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (refreshToken is not null)
            request.Headers.Add("Cookie", $"{Cookie}={refreshToken}");
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string? refreshToken) =>
        client.PostWithCookieAsync("/api/auth/refresh", refreshToken);

    public static string SetCookie(this HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(Cookie + "="));

    public static string RefreshToken(this HttpResponseMessage response) =>
        response.SetCookie().Split(';')[0][(Cookie.Length + 1)..];

    public static async Task<(string AccessToken, string RefreshToken)> TokensAsync(this HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        return (body.AccessToken, response.RefreshToken());
    }

    public static Task<HttpResponseMessage> SendWithBearerAsync(this HttpClient client, HttpMethod method, string path,
        string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = body is null ? null : JsonContent.Create(body),
        };
        return client.SendAsync(request);
    }
}
