using EnergyManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyManagementApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();
}