using System.Text;
using Microsoft.Extensions.Options;

namespace LaMeridienneAPI.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();
        if (Encoding.UTF8.GetByteCount(options.Key) < 32)
            failures.Add("Jwt__Key est absente ou fait moins de 32 octets.");
        if (string.IsNullOrWhiteSpace(options.Issuer))
            failures.Add("Jwt__Issuer est absente.");
        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("Jwt__Audience est absente.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
