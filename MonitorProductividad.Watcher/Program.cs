using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonitorProductividad.Watcher;

var builder = Host.CreateApplicationBuilder(args);

// 1. Registramos tu nuevo 'WatchdogWorker' en lugar del 'Worker' eliminado
builder.Services.AddHostedService<WatchdogWorker>();

// 2. Activamos el soporte nativo para el Service Control Manager (SCM) de Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "WatcherMP";
});

var host = builder.Build();
host.Run();