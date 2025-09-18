using EnergyManagement.Application.Analytics.Domain;
using EnergyManagement.Application.Analytics.Services;
using EnergyManagement.Application.Analytics.Services.MachineLearning;
using EnergyManagement.Application.Devices.Domain;
using EnergyManagement.Application.Devices.Services;
using EnergyManagement.Application.Sensors.Services;
using EnergyManagement.Application.Users.Domain;
using EnergyManagement.Application.Users.Services;
using EnergyManagement.Infraestructure.Data;
using EnergyManagement.Infraestructure.Repositories;
using EnergyManagement.Infraestructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EnergyManagement.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Identity
        services.AddIdentity<User, IdentityRole<int>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;

            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthApplicationService, AuthApplicationService>();
        services.AddScoped<IDeviceApplicationService, DeviceApplicationService>();
        services.AddScoped<ISensorDataService, SensorDataService>();

        return services;
    }

    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services)
    {
        // Repositórios
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

        // Serviços de Machine Learning
        services.AddScoped<IClusteringService, ClusteringService>();
        services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
        services.AddScoped<IPredictionService, PredictionService>();

        // Serviço principal de Analytics
        services.AddScoped<IAnalyticsApplicationService, AnalyticsApplicationService>();

        return services;
    }
}
