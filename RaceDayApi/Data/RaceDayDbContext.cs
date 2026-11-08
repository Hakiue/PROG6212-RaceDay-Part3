using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Users
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasMaxLength(20).IsRequired();
            e.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(50).IsRequired();
            e.Property(u => u.Email).HasMaxLength(100).IsRequired();
        });

        // EventTypes
        modelBuilder.Entity<EventType>(e =>
        {
            e.HasIndex(t => t.TypeName).IsUnique();
            e.Property(t => t.TypeName).HasMaxLength(20).IsRequired();
        });

        // Events
        modelBuilder.Entity<Event>(e =>
        {
            e.Property(ev => ev.DistanceKm).HasColumnType("decimal(6,2)");
            e.Property(ev => ev.Name).HasMaxLength(100).IsRequired();
            e.Property(ev => ev.Location).HasMaxLength(150).IsRequired();

            e.HasOne(ev => ev.Organiser)
                .WithMany(u => u.OrganisedEvents)
                .HasForeignKey(ev => ev.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ev => ev.EventType)
                .WithMany(t => t.Events)
                .HasForeignKey(ev => ev.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Categories
        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.DistanceKm).HasColumnType("decimal(6,2)");
            e.Property(c => c.Name).HasMaxLength(50).IsRequired();

            e.HasOne(c => c.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Enrolments
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.Property(en => en.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(en => new { en.ParticipantId, en.EventId }).IsUnique();

            e.HasOne(en => en.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(en => en.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(en => en.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(en => en.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(en => en.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(en => en.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Results
        modelBuilder.Entity<Result>(e =>
        {
            e.HasIndex(r => r.EnrolmentId).IsUnique();

            e.HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.RecordedByOrganiser)
                .WithMany()
                .HasForeignKey(r => r.RecordedByOrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
