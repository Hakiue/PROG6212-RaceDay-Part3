using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies pending EF Core migrations (retrying while a freshly started SQL Server
    /// container comes up) and seeds the three event types if the table is empty.
    /// </summary>
    public static async Task InitialiseAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        if (db.Database.IsRelational())
        {
            const int maxAttempts = 10;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    break;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning(ex, "Database not ready (attempt {Attempt} of {Max}). Retrying in 3 seconds...", attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
        }

        if (!await db.EventTypes.AnyAsync())
        {
            db.EventTypes.AddRange(
                new EventType { TypeName = "Run" },
                new EventType { TypeName = "Walk" },
                new EventType { TypeName = "Cycle" });
            await db.SaveChangesAsync();
        }
    }
}
