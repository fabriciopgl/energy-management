namespace EnergyManagementApi.Services;

public interface IMqttClientService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
