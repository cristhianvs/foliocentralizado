using FolioMonitor.Worker.Services;
using Microsoft.Extensions.Configuration;
using System.Threading; // Required for CancellationToken
using System; // Required for TimeSpan

namespace FolioMonitor.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider; // To scope services per execution
    private readonly TimeSpan _checkInterval;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        var intervalHours = configuration.GetValue<double?>("WorkerSettings:CheckIntervalHours") ?? 24.0;
        _checkInterval = TimeSpan.FromHours(intervalHours);
        _logger.LogInformation("Worker check interval set to {IntervalHours} hours.", intervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Folio Monitoring Worker starting at: {time}", DateTimeOffset.Now);

        using var timer = new PeriodicTimer(_checkInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested. Shutting down worker.");
                break;
            }

            _logger.LogInformation("Worker executing check at: {time}", DateTimeOffset.Now);

            try
            {
                // Create a scope for this execution to resolve scoped services
                using var scope = _serviceProvider.CreateScope();
                var queryService = scope.ServiceProvider.GetRequiredService<ISourceFolioQueryService>();
                var apiClient = scope.ServiceProvider.GetRequiredService<IApiClientService>();

                // Fetch data for both modules
                var invoiceData = await queryService.GetFolioDataAsync("MOD28");
                var creditNoteData = await queryService.GetFolioDataAsync("MOD29");

                // Send data to the API
                await apiClient.SendFolioDataAsync(invoiceData, creditNoteData);

                _logger.LogInformation("Worker check completed successfully at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the worker execution cycle.");
                // Decide if the error should stop the worker or just log and continue
            }
             _logger.LogInformation("Next check scheduled in {CheckInterval}", _checkInterval);
        }

        _logger.LogInformation("Folio Monitoring Worker stopping at: {time}", DateTimeOffset.Now);
    }
}
