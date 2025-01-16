using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GeeksCoreLibrary.Core.Services;

    /// <summary>
    /// Hosting services that inherit this service class will run in a scoped setting instead of singleton,
    /// allowing scoped services (such as DatabaseConnection) to be used.
/// </summary>
public class ScopedBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    public ScopedBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DoWorkAsync(stoppingToken);
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedProcessingServices = scope.ServiceProvider.GetServices<IScopedProcessingService>();

        await Task.WhenAll(scopedProcessingServices.Select(scopedProcessingService => scopedProcessingService.DoWorkAsync(stoppingToken)).ToArray());
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await base.StopAsync(stoppingToken);
    }
}