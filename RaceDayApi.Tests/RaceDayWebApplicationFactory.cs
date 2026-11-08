using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Tests;

/// <summary>
/// Spins up the real API in-memory for testing, swapping the SQL Server DbContext
/// for an EF Core InMemory provider so tests never touch the real RaceDayDB.
/// Each instance gets its own uniquely-named database so tests don't collide.
/// </summary>
public class RaceDayWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RaceDayTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Never talk to real Azure from tests: swap in an in-memory fake blob service
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();

            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            // EF Core (5+) registers each AddDbContext call's configuration as an
            // internal IDbContextOptionsConfiguration<T> entry, and APPLIES ALL of
            // them when building options - so the original SQL Server config from
            // Program.cs would otherwise still be applied alongside our InMemory one
            // below, causing "only a single database provider can be registered".
            // That type is internal to EF Core, so we find it by name via reflection
            // rather than referencing it directly.
            var optionsConfigDescriptors = services.Where(d =>
                d.ServiceType.IsGenericType &&
                d.ServiceType.Name == "IDbContextOptionsConfiguration`1" &&
                d.ServiceType.GenericTypeArguments[0] == typeof(RaceDayDbContext)
            ).ToList();
            foreach (var d in optionsConfigDescriptors) services.Remove(d);

            services.AddDbContext<RaceDayDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Seed EventTypes so tests can create events without a separate setup step
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.Database.EnsureCreated();

            if (!db.EventTypes.Any())
            {
                db.EventTypes.AddRange(
                    new EventType { TypeName = "Run" },
                    new EventType { TypeName = "Walk" },
                    new EventType { TypeName = "Cycle" });
                db.SaveChanges();
            }
        });
    }
}
