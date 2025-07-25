using EnergyManagement.Application.Sensors.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagement.Infraestructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
}