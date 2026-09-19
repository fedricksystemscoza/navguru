using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NavGuru.Models;

namespace NavGuru.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<OrientationEvent> OrientationEvents => Set<OrientationEvent>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<FaqEntry> FaqEntries => Set<FaqEntry>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SupportResource> SupportResources => Set<SupportResource>();
    public DbSet<CampusLocation> CampusLocations => Set<CampusLocation>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OrientationEvent>()
            .HasIndex(e => e.CheckInToken)
            .IsUnique();

        builder.Entity<CheckIn>()
            .HasIndex(c => new { c.UserId, c.OrientationEventId })
            .IsUnique();   // one check-in per student per event
    }
}
