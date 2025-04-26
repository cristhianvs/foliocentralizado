using FolioMonitor.Web.Components;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HttpClient for API calls
var apiSettings = builder.Configuration.GetSection("FolioApi");
var baseAddress = apiSettings.GetValue<string>("BaseUrl");
var apiKey = apiSettings.GetValue<string>("ApiKey");

if (string.IsNullOrEmpty(baseAddress) || string.IsNullOrEmpty(apiKey))
{
    // Log or handle missing configuration appropriately
    Console.WriteLine("Warning: FolioApi BaseUrl or ApiKey not configured in appsettings.json for FolioMonitor.Web");
}
else
{
    builder.Services.AddHttpClient("FolioApiClient", client =>
    {
        client.BaseAddress = new Uri(baseAddress);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
    });
    // Add HttpMessageHandler configuration if needed (e.g., ignore SSL errors in dev, NOT recommended for prod)
    // .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    // {
    //     ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
    // });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
