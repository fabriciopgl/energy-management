using EnergyManagement.Application.Devices.Domain;
using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Application.Users.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagement.Infraestructure.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            entity.HasKey(e => e.Id);

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

            // Relacionamento Device -> User
            entity.HasOne(d => d.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SensorReading Configuration - VERSÃO CORRIGIDA
        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configurar propriedades
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.Property(e => e.Current)
                .IsRequired();

            entity.Property(e => e.Voltage)
                .IsRequired();

            entity.Property(e => e.Power)
                .IsRequired();

            entity.Property(e => e.Energy)
                .IsRequired();

            entity.Property(e => e.Rssi)
                .IsRequired();

            entity.Property(e => e.FreeHeap)
                .IsRequired();

            // CONFIGURAÇÃO CORRETA DO RELACIONAMENTO
            entity.Property(e => e.DeviceId)
                .IsRequired(false); // Pode ser null temporariamente

            entity.HasOne(e => e.Device)
                .WithMany(d => d.SensorReadings)
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Índices para performance
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_SensorReadings_Timestamp");

            entity.HasIndex(e => e.DeviceId)
                .HasDatabaseName("IX_SensorReadings_DeviceId");

            entity.HasIndex(e => new { e.DeviceId, e.Timestamp })
                .HasDatabaseName("IX_SensorReadings_DeviceId_Timestamp");
        });

        // UserPreference Configuration
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.Id);

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
}