using LaMeridienneAPI.Auth;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Infrastructure;
using LaMeridienneAPI.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var envFile = builder.Configuration["ENV_FILE"] ?? ".env";
if (envFile.Length > 0)
    builder.Configuration.AddDotEnvFile(Path.Combine(builder.Environment.ContentRootPath, envFile));

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Connexion PostgreSQL absente : renseignez POSTGRES_HOST, POSTGRES_DB, POSTGRES_USER et POSTGRES_PASSWORD dans .env (modèle : .env.example).");

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

builder.Services.AddHostedService<Migrator>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddFrenchProblemDetails();
builder.Services.AddLaMeridienneAuth(builder.Configuration);
builder.Services.AddScoped<SheetService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<EntryService>();
builder.Services.AddScoped<LayoutService>();
builder.Services.AddScoped<NewspaperService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddControllers(o =>
        o.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(AuthSetup.ClientCors);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers().RequireAuthorization(AuthSetup.AdminPolicy);

app.Run();

public partial class Program;
