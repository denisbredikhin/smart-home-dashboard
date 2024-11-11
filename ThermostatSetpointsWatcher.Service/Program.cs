using Serilog;
using ThermostatSetpointsWatcher.Core;
using ThermostatSetpointsWatcher.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddCommandLine(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Tado - Viessmann integration service";
});

var logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TadoViessmannIntegrationService", "logs", "SyncService.log");

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddTransient(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var port = configuration["port"];
    var logger = provider.GetRequiredService<ILogger<TadoViessmanSynchronizer>>();
    return new TadoViessmanSynchronizer(port, logger);
});
builder.Services.AddHostedService<TadoViessmanSyncWorker>();

var host = builder.Build();
host.Run();
     