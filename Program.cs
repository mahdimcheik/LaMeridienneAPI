using LaMeridienneAPI.Auth;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:Default est absente. En développement : dotnet user-secrets set \"ConnectionStrings:Default\" \"…\".");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<ShopOptions>().BindConfiguration(ShopOptions.Section).ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<ShopOptions>, ShopOptionsValidator>();
builder.Services.AddSingleton<ShopClock>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, o) => o
    .UseNpgsql(connectionString)
    .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddFrenchProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
