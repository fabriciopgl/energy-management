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
builder.Services.AddAnalyticsServices();

// CORS (para frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Política específica para desenvolvimento local
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",     // React dev server
                "http://localhost:5173",     // Vite dev server  
                "http://127.0.0.1:5500",     // Live Server VSCode
                "null"                       // Arquivos locais
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowed(origin => true); // Permite qualquer origem em dev
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

    app.UseCors("DevelopmentCors");
}
else
{
    app.UseCors("AllowAll");
}


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