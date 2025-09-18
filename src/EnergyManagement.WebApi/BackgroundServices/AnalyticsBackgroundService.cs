using EnergyManagement.Application.Analytics.Services;

namespace EnergyManagement.WebApi.BackgroundServices;

public class AnalyticsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalyticsBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Executar a cada 6 horas

    public AnalyticsBackgroundService(IServiceProvider serviceProvider, ILogger<AnalyticsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPeriodicAnalysis();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante execução automática de análise");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunPeriodicAnalysis()
    {
        using var scope = _serviceProvider.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsApplicationService>();

        _logger.LogInformation("Iniciando análise automática de ML");

        // Em um cenário real, você obteria a lista de usuários do banco
        // Por simplicidade, vamos assumir que será executado sob demanda
        // ou implementado de forma diferente conforme a necessidade

        _logger.LogInformation("Análise automática concluída");
    }
}
