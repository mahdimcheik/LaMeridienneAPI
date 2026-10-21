using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class AuthTests(PostgresFixture db)
{
    private readonly FakeTimeProvider _time = new(TimeProvider.System.GetUtcNow());

    private Task<ApiFactory> FactoryAsync(Action<ApiFactory>? configure = null) =>
        ApiFactory.CreateAsync(db.ConnectionStringFor("auth_" + Guid.NewGuid().ToString("N")), configure, _time);

    private static async Task<ProblemDetails> ProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
    }

    [Fact]
    public async Task US_E01_01_la_connexion_rend_un_jeton_qui_ouvre_les_autres_routes()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();

        var response = await client.LoginAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Assert.Equal(_time.GetUtcNow().AddMinutes(15), body.ExpiresAt);
        Assert.Equal(("marie", "Marie"), (body.User.UserName, body.User.DisplayName));

        var me = await client.SendWithBearerAsync(HttpMethod.Get, "/api/auth/me", body.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(body.User, await me.Content.ReadFromJsonAsync<UserDto>());
    }

    [Fact]
    public async Task US_E01_01_identifiant_inconnu_et_mauvais_mot_de_passe_ont_le_meme_message()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();

        var unknown = await ProblemAsync(await client.LoginAsync("personne", "mot-de-passe-marie"), HttpStatusCode.Unauthorized);
        var wrong = await ProblemAsync(await client.LoginAsync("marie", "mauvais-mot-de-passe"), HttpStatusCode.Unauthorized);

        Assert.Equal(unknown.Detail, wrong.Detail);
        Assert.Equal(unknown.Title, wrong.Title);
    }

    [Theory]
    [InlineData("GET", "/api/auth/me")]
    [InlineData("PUT", "/api/account/password")]
    public async Task US_E01_01_sans_jeton_valide_une_route_protegee_repond_401(string method, string path)
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();
        var (access, _) = await (await client.LoginAsync()).TokensAsync();

        var anonymous = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        var forged = await client.SendWithBearerAsync(new HttpMethod(method), path, access[..^4] + "AAAA");
        _time.Advance(TimeSpan.FromMinutes(16));
        var expired = await client.SendWithBearerAsync(new HttpMethod(method), path, access);

        foreach (var response in new[] { anonymous, forged, expired })
            Assert.Equal("Authentification requise", (await ProblemAsync(response, HttpStatusCode.Unauthorized)).Title);
    }

    [Fact]
    public async Task US_E01_02_le_renouvellement_tourne_et_l_ancien_jeton_ne_sert_plus()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();
        var (_, first) = await (await client.LoginAsync()).TokensAsync();

        var renewed = await client.RefreshAsync(first);
        var (access, second) = await renewed.TokensAsync();

        Assert.NotEqual(first, second);
        Assert.Equal(HttpStatusCode.OK,
            (await client.SendWithBearerAsync(HttpMethod.Get, "/api/auth/me", access)).StatusCode);
        await ProblemAsync(await client.RefreshAsync(first), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task US_E01_02_le_navigateur_renouvelle_avec_son_cookie()
    {
        await using var factory = await FactoryAsync();
        var (client, _) = await factory.LoginAsync();

        var first = await client.PostAsync("/api/auth/refresh", null);
        var second = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task R_03_reutiliser_un_jeton_revoque_toutes_les_sessions_du_compte()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();
        var (_, a1) = await (await client.LoginAsync()).TokensAsync();
        var (_, b1) = await (await client.LoginAsync()).TokensAsync();
        var (_, other) = await (await client.LoginAsync(1)).TokensAsync();
        var (_, a2) = await (await client.RefreshAsync(a1)).TokensAsync();

        await ProblemAsync(await client.RefreshAsync(a1), HttpStatusCode.Unauthorized);

        await ProblemAsync(await client.RefreshAsync(a2), HttpStatusCode.Unauthorized);
        await ProblemAsync(await client.RefreshAsync(b1), HttpStatusCode.Unauthorized);
        Assert.Equal(HttpStatusCode.OK, (await client.RefreshAsync(other)).StatusCode);
    }

    [Fact]
    public async Task R_03_le_renouvellement_glisse_puis_expire_apres_30_jours_sans_usage()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();
        var (_, token) = await (await client.LoginAsync()).TokensAsync();

        _time.Advance(TimeSpan.FromDays(29));
        (_, token) = await (await client.RefreshAsync(token)).TokensAsync();
        _time.Advance(TimeSpan.FromDays(29));
        (_, token) = await (await client.RefreshAsync(token)).TokensAsync();
        _time.Advance(TimeSpan.FromDays(30));

        await ProblemAsync(await client.RefreshAsync(token), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task US_E01_02_sans_cookie_ou_avec_un_jeton_inconnu_le_renouvellement_repond_401()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();

        await ProblemAsync(await client.RefreshAsync(null), HttpStatusCode.Unauthorized);
        await ProblemAsync(await client.RefreshAsync("inconnu"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task US_E01_03_la_deconnexion_revoque_le_jeton_efface_le_cookie_et_est_idempotente()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();
        var (_, token) = await (await client.LoginAsync()).TokensAsync();

        var logout = await client.PostWithCookieAsync("/api/auth/logout", token);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var cleared = logout.SetCookie().ToLowerInvariant();
        Assert.StartsWith(AuthHttp.Cookie + "=;", cleared);
        Assert.Contains("expires=thu, 01 jan 1970", cleared);
        Assert.Contains("path=/api/auth", cleared);
        await ProblemAsync(await client.RefreshAsync(token), HttpStatusCode.Unauthorized);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostWithCookieAsync("/api/auth/logout", token)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostWithCookieAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task ADR_0002_le_cookie_de_renouvellement_est_httponly_secure_strict_et_limite_a_l_authentification()
    {
        await using var factory = await FactoryAsync();

        var cookie = (await factory.RawClient().LoginAsync()).SetCookie().ToLowerInvariant();

        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("path=/api/auth", cookie);
    }

    [Fact]
    public async Task R_02_cinq_echecs_verrouillent_le_compte_quinze_minutes()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync("marie", "mauvais-mot-de-passe")).StatusCode);

        var locked = await ProblemAsync(await client.LoginAsync(), (HttpStatusCode)423);
        Assert.Equal("Compte verrouillé", locked.Title);
        Assert.Contains("Réessayez", locked.Detail);
        Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync(1)).StatusCode);

        _time.Advance(TimeSpan.FromMinutes(14));
        Assert.Equal((HttpStatusCode)423, (await client.LoginAsync()).StatusCode);
        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync()).StatusCode);
    }

    [Fact]
    public async Task R_02_une_connexion_reussie_remet_le_compteur_a_zero()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();

        for (var round = 0; round < 2; round++)
        {
            for (var i = 0; i < 4; i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync("marie", "mauvais-mot-de-passe")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.LoginAsync()).StatusCode);
        }
    }

    [Fact]
    public async Task La_connexion_est_limitee_a_10_requetes_par_minute()
    {
        await using var factory = await FactoryAsync();
        var client = factory.RawClient();

        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.LoginAsync("personne", "x")).StatusCode);
        var rejected = await client.LoginAsync();

        Assert.Equal("Trop de requêtes", (await ProblemAsync(rejected, HttpStatusCode.TooManyRequests)).Title);
        Assert.True(rejected.Headers.RetryAfter is not null);
    }

    [Fact]
    public async Task Une_requete_invalide_repond_400_par_champ()
    {
        await using var factory = await FactoryAsync();

        var response = await factory.RawClient().PostAsJsonAsync("/api/auth/login", new { userName = "marie" });

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["password"], problem!.Errors.Keys);
    }

    [Fact]
    public async Task CORS_autorise_le_client_de_developpement_avec_identifiants()
    {
        await using var factory = await FactoryAsync(f => f.Settings["Cors:Origins:0"] = "http://localhost:4200");
        var client = factory.RawClient();

        async Task<HttpResponseMessage> Preflight(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/refresh");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            return await client.SendAsync(request);
        }

        var allowed = await Preflight("http://localhost:4200");
        Assert.Equal("http://localhost:4200", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", allowed.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.False((await Preflight("https://ailleurs.example")).Headers.Contains("Access-Control-Allow-Origin"));
    }
}
