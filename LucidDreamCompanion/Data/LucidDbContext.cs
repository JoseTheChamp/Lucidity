using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidDreamCompanion.Data;

public class LucidDbContext : DbContext
{
    public DbSet<DreamEntry> DreamEntries => Set<DreamEntry>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<SleepSession> SleepSessions => Set<SleepSession>();
    public DbSet<AudioCue> AudioCues => Set<AudioCue>();
    public DbSet<AudioAsset> AudioAssets => Set<AudioAsset>();
    public DbSet<CueProfile> CueProfiles => Set<CueProfile>();
    public DbSet<ScheduledCueLog> ScheduledCueLogs => Set<ScheduledCueLog>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<RealityCheckLog> RealityCheckLogs => Set<RealityCheckLog>();
    public DbSet<JournalReminder> JournalReminders => Set<JournalReminder>();

    public LucidDbContext(DbContextOptions<LucidDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        b.Entity<DreamEntry>()
            .HasMany(e => e.Tags)
            .WithMany(t => t.Entries)
            .UsingEntity(j => j.ToTable("DreamEntryTags"));

        b.Entity<DreamEntry>()
            .HasOne(e => e.SleepSession)
            .WithMany(s => s.Entries)
            .HasForeignKey(e => e.SleepSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<DreamEntry>().HasIndex(e => e.DreamDate);
        b.Entity<DreamEntry>().HasIndex(e => e.CreatedAt);

        b.Entity<AudioCue>()
            .HasOne(c => c.AudioAsset)
            .WithMany()
            .HasForeignKey(c => c.AudioAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<AudioCue>()
            .HasOne(c => c.CueProfile)
            .WithMany(p => p.Cues)
            .HasForeignKey(c => c.CueProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<SleepSession>()
            .HasOne(s => s.TechniqueProfile)
            .WithMany()
            .HasForeignKey(s => s.TechniqueProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<ScheduledCueLog>()
            .HasOne(l => l.SleepSession)
            .WithMany(s => s.CueLogs)
            .HasForeignKey(l => l.SleepSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<ScheduledCueLog>().HasIndex(l => l.ScheduledFor);

        b.Entity<AppSettings>()
            .HasData(new AppSettings { Id = 1 });
    }
}
