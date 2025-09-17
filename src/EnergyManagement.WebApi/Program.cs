using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Core.Services;
using EnergyManagement.Infraestructure.Data;
using EnergyManagement.Infraestructure.Repositories;
using EnergyManagement.Infraestructure.Services;
using EnergyManagement.WebApi.BackgroundServices;
using EnergyManagement.WebApi.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (Identity, JWT, Repositories, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// Existing services (mantendo o que já funcionava)
builder.Services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
builder.Services.AddHostedService<MqttBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// CORS (para frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000") // Next.js default
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Energy Management API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Garante que o banco exista e aplica migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();