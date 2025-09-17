using EnergyManagement.Core.Services;

namespace EnergyManagement.WebApi.BackgroundServices;

public class MqttBackgroundService(IMqttClientService mqttService, ILogger<MqttBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Iniciando MQTT Background Service");
            await mqttService.StartAsync(stoppingToken);

            // Manter o serviço rodando até ser cancelado
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("MQTT Background Service foi cancelado");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no MQTT Background Service");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Parando MQTT Background Service");
        await mqttService.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}