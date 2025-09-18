using EnergyManagement.Application.Sensors.Domain;
using EnergyManagement.Application.Sensors.Models.Dtos;
using EnergyManagement.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text.Json;

namespace EnergyManagement.Infraestructure.Services;

public class MqttClientService : IMqttClientService, IDisposable
{
    private readonly IManagedMqttClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttClientService> _logger;

    public MqttClientService(IServiceScopeFactory scopeFactory, ILogger<MqttClientService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var factory = new MqttFactory();
        _client = factory.CreateManagedMqttClient();

        // Configurar eventos
        _client.ConnectedAsync += OnConnectedAsync;
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var options = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .WithCleanSession(true)
                .Build())
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .Build();

        await _client.StartAsync(options);
        _logger.LogInformation("MQTT Client iniciado");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _client.StopAsync();
        _logger.LogInformation("MQTT Client parado");
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
    {
        _logger.LogInformation("Conectado ao broker MQTT");

        // Subscrever ao tópico
        var topicFilter = new MqttTopicFilterBuilder()
            .WithTopic("energy/sensor1")
            .Build();

        await _client.SubscribeAsync([topicFilter]);
        _logger.LogInformation("Subscrito ao tópico: energy/sensor1");
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = e.ApplicationMessage.PayloadSegment.ToArray();
            var json = System.Text.Encoding.UTF8.GetString(payload);

            _logger.LogDebug("Mensagem recebida: {Message}", json);

            var dto = JsonSerializer.Deserialize<SensorReadingDto>(json);
            if (dto is not null)
            {
                var reading = new SensorReading
                {
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(dto.Timestamp).UtcDateTime,
                    Current = dto.Current,
                    Voltage = dto.Voltage,
                    Power = dto.Power,
                    Energy = dto.Energy,
                    Rssi = dto.Rssi,
                    FreeHeap = dto.FreeHeap,
                    DeviceId = 1
                };

                // Criar um novo scope para usar o repository
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ISensorReadingRepository>();

                await repo.AddAsync(reading);

                _logger.LogInformation("Leitura do sensor salva: {Timestamp}", reading.Timestamp);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar mensagem JSON: {Payload}",
                System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem MQTT");
        }
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogWarning("Desconectado do broker MQTT: {Reason}", e.Reason);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}