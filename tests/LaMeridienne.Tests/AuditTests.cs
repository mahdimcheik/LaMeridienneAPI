using LaMeridienneAPI.Auth;
using LaMeridienneAPI.Data;
using LaMeridienneAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace LaMeridienne.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class AuditTests(PostgresFixture db) : IAsyncLifetime
{
    public sealed class Probe : AuditableEntity, ISoftDeletable
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = "";
        public DateTimeOffset? DeletedAt { get; set; }
    }

    private sealed class ProbeDbContext(DbContextOptions options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Probe>();
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class FixedUser(Guid? id) : ICurrentUser
    {
        public Guid? UserId { get; set; } = id;
    }

    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-21T08:00:00Z"));
    private readonly FixedUser _user = new(Guid.NewGuid());
    private readonly string _connectionString = db.ConnectionStringFor("audit_" + Guid.NewGuid().ToString("N"));

    private ProbeDbContext NewContext() => new(new DbContextOptionsBuilder<ProbeDbContext>()
        .UseNpgsql(_connectionString)
        .AddInterceptors(new AuditInterceptor(_time, _user))
        .Options);

    public async Task InitializeAsync()
    {
        await using var ctx = NewContext();
        await ctx.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await using var ctx = NewContext();
        await ctx.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task US_E09_03_creation_et_modification_sont_horodatees_et_attribuees()
    {
        var creator = _user.UserId;
        var probe = new Probe { Label = "a" };
        await using (var ctx = NewContext())
        {
            ctx.Add(probe);
            await ctx.SaveChangesAsync();
        }

        _time.Advance(TimeSpan.FromMinutes(5));
        _user.UserId = Guid.NewGuid();
        await using (var ctx = NewContext())
        {
            var loaded = await ctx.Set<Probe>().SingleAsync(p => p.Id == probe.Id);
            loaded.Label = "b";
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            var saved = await ctx.Set<Probe>().SingleAsync(p => p.Id == probe.Id);
            Assert.Equal(DateTimeOffset.Parse("2026-10-21T08:00:00Z"), saved.CreatedAt);
            Assert.Equal(creator, saved.CreatedById);
            Assert.Equal(DateTimeOffset.Parse("2026-10-21T08:05:00Z"), saved.UpdatedAt);
            Assert.Equal(_user.UserId, saved.UpdatedById);
        }
    }

    [Fact]
    public async Task R_06_une_suppression_est_logique_et_masquee_des_lectures()
    {
        var probe = new Probe { Label = "a" };
        await using (var ctx = NewContext())
        {
            ctx.Add(probe);
            await ctx.SaveChangesAsync();
            ctx.Remove(probe);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext())
        {
            Assert.False(await ctx.Set<Probe>().AnyAsync());
            var raw = await ctx.Set<Probe>().IgnoreQueryFilters().SingleAsync(p => p.Id == probe.Id);
            Assert.Equal(_time.GetUtcNow(), raw.DeletedAt);
        }
    }
}
