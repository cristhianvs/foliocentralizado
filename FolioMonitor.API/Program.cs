using FolioMonitor.API.Data;
using FolioMonitor.Core.Interfaces;
using FolioMonitor.API.Repositories;
using FolioMonitor.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Folio Monitor API",
        Description = "API for monitoring fiscal folio availability."
    });

    // Set the comments path for the Swagger JSON and UI.
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    // Optional: Add security definition for API Key
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. X-API-KEY: My_API_Key",
        In = ParameterLocation.Header,
        Name = "X-API-KEY", // Match header name in middleware
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                 Scheme = "ApiKey",
                 Name = "ApiKey",
                 In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Register Repositories
builder.Services.AddScoped<IFolioHistoryRepository, FolioHistoryRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();

// Register Services
builder.Services.AddScoped<IAlertService, AlertService>();
// Register actual Email Notification Service (Task 7)
builder.Services.AddScoped<INotificationService, EmailNotificationService>();

// Add DbContext configuration ONLY if not running in the "Testing" environment
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    // Configure for MySQL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, 
                         ServerVersion.AutoDetect(connectionString), // Auto-detect server version
                         mySqlOptions => mySqlOptions.EnableRetryOnFailure( // Optional: Configure resilience
                             maxRetryCount: 5,
                             maxRetryDelay: TimeSpan.FromSeconds(30),
                             errorNumbersToAdd: null)));
}

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable middleware to serve generated Swagger as a JSON endpoint.
app.UseSwagger();
// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
// specifying the Swagger JSON endpoint.
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Folio Monitor API v1");
    // Serve Swagger UI at the app's root (e.g. http://localhost:<port>/)
    options.RoutePrefix = string.Empty; 
});

app.UseHttpsRedirection();

// Add API Key Middleware ONLY if not in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseMiddleware<FolioMonitor.API.Middleware.ApiKeyAuthMiddleware>();
}

app.UseAuthorization();

app.MapControllers();

// Remove or comment out the default weather forecast endpoint if not needed
/*
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();
*/

app.Run();

// Add public partial class declaration for test visibility
public partial class Program { }

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
