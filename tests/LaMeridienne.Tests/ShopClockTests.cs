using LaMeridienneAPI.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

public sealed class ShopClockTests
{
    private static ShopClock ClockAt(string utc) =>
        new(new FakeTimeProvider(DateTimeOffset.Parse(utc)), Options.Create(new ShopOptions { TimeZone = "Europe/Paris" }));

    [Theory]
    [InlineData("2026-10-21T21:59:00Z", "2026-10-21")] // heure d'été, UTC+2 : 23:59 à Paris
    [InlineData("2026-10-21T22:00:00Z", "2026-10-22")] // minuit à Paris
    [InlineData("2026-12-31T22:59:00Z", "2026-12-31")] // heure d'hiver, UTC+1 : 23:59 à Paris
    [InlineData("2026-12-31T23:00:00Z", "2027-01-01")]
    public void R_01_aujourd_hui_suit_le_fuseau_de_Paris(string utc, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), ClockAt(utc).Today());

    [Theory]
    [InlineData("")]
    [InlineData("Mars/Olympus")]
    public void Un_fuseau_absent_ou_inconnu_est_refuse(string zone) =>
        Assert.True(new ShopOptionsValidator().Validate(null, new ShopOptions { TimeZone = zone }).Failed);
}
