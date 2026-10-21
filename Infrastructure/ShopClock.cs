using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace LaMeridienneAPI.Infrastructure;

public sealed class ShopOptions
{
    public const string Section = "Shop";

    [Required]
    public string TimeZone { get; set; } = "";
}

/// <summary>« Aujourd'hui » dans le fuseau de la boutique (R-01), jamais celui du serveur ni du navigateur.</summary>
public sealed class ShopClock(TimeProvider time, IOptions<ShopOptions> options)
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), _zone).DateTime);
}

public sealed class ShopOptionsValidator : IValidateOptions<ShopOptions>
{
    public ValidateOptionsResult Validate(string? name, ShopOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TimeZone))
            return ValidateOptionsResult.Fail("Shop:TimeZone est obligatoire.");
        return TimeZoneInfo.TryFindSystemTimeZoneById(options.TimeZone, out _)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"Shop:TimeZone « {options.TimeZone} » est un fuseau inconnu.");
    }
}
