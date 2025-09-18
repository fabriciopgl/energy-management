using EnergyManagement.Application.Analytics.Domain;
using EnergyManagement.Application.Devices.Domain;
using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Application.Users.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagement.Infraestructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<ConsumptionPattern> ConsumptionPatterns => Set<ConsumptionPattern>();
    public DbSet<AnomalyDetection> AnomalyDetections => Set<AnomalyDetection>();
    public DbSet<EnergyRecommendation> EnergyRecommendations => Set<EnergyRecommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações existentes mantidas...
        ConfigureDomainEntities(modelBuilder);

        // Novas configurações para Analytics
        ConfigureAnalyticsEntities(modelBuilder);
    }

    private static void ConfigureDomainEntities(ModelBuilder modelBuilder)
    {
        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        // Device Configuration
        modelBuilder.Entity<Device>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.MacAddress)
                .HasMaxLength(17)
                .IsRequired();

            entity.Property(e => e.Location)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.MacAddress)
                .IsUnique();

            entity.HasOne(d => d.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SensorReading Configuration
        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.DeviceId);

            entity.HasOne<Device>()
                .WithMany(d => d.SensorReadings)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // UserPreference Configuration
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.Property(e => e.Key)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Value)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(p => p.User)
                .WithMany(u => u.Preferences)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.Key })
                .IsUnique();
        });
    }

    private static void ConfigureAnalyticsEntities(ModelBuilder modelBuilder)
    {
        // ConsumptionPattern Configuration
        modelBuilder.Entity<ConsumptionPattern>(entity =>
        {
            entity.Property(e => e.PatternType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.DeviceNames)
                .HasMaxLength(2000);

            entity.Property(e => e.AnalyzedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.PatternType, e.AnalyzedAt });
        });

        // AnomalyDetection Configuration
        modelBuilder.Entity<AnomalyDetection>(entity =>
        {
            entity.Property(e => e.AnomalyType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.DetectedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsResolved, e.DetectedAt });
            entity.HasIndex(e => new { e.DeviceId, e.DetectedAt });
        });

        // EnergyRecommendation Configuration
        modelBuilder.Entity<EnergyRecommendation>(entity =>
        {
            entity.Property(e => e.RecommendationType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsApplied, e.CreatedAt });
        });
    }
}