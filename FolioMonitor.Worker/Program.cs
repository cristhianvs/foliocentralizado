using FolioMonitor.Worker;
using FolioMonitor.Worker.Services;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Folio Monitoring Service";
});

// Register custom services
builder.Services.AddScoped<ISourceFolioQueryService, SourceFolioQueryService>();

// Register HttpClient for ApiClientService
builder.Services.AddHttpClient<IApiClientService, ApiClientService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
