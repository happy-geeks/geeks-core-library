using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using Microsoft.Extensions.Options;
using GeeksCoreLibrary.Modules.MessageBroker.Enums;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.MessageBroker.Services;

/// <inheritdoc cref="GeeksCoreLibrary.Modules.MessageBroker.Services.IMessageService" />
public class RabbitMessageService : IMessageService, IScopedService, ISingletonService, IDisposable
{
    private readonly ILogger<RabbitMessageService> logger;
    private readonly GclSettings gclSettings;
    private IBus bus;

    public RabbitMessageService(IOptions<GclSettings> gclSettings, ILogger<RabbitMessageService> logger)
    {
        this.logger = logger;
        this.gclSettings = gclSettings.Value;
    }
    
    /// <inheritdoc />
    public async Task SendAsync<T>(string queue, T message, CancellationToken cancellationToken = default)
    {
        if (gclSettings.MessageBroker != MessageBrokers.RabbitMq)
        {
            logger.LogDebug("Send in RabbitMessageService called but message broker has not been set to RabbitMQ");
            return;
        }
        
        bus ??= RabbitHutch.CreateBus(this.gclSettings.MessageBrokerConnectionString);

        logger.LogInformation($"Sending message via RabbitMQ message queue \"{queue}\". The message was: {message}");
        await bus.SendReceive.SendAsync(queue, message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReceiveAsync<T>(string queue, Func<T, Task> onMessage, CancellationToken cancellationToken = default)
    {
        if (gclSettings.MessageBroker != MessageBrokers.RabbitMq)
        {
            logger.LogDebug("Send in RabbitMessageService called but message broker has not been set to RabbitMQ");
            return;
        }
        
        bus ??= RabbitHutch.CreateBus(this.gclSettings.MessageBrokerConnectionString);

        logger.LogInformation($"Started listening on message queue \"{queue}\"");
        await bus.SendReceive.ReceiveAsync<T>(queue, onMessage, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        logger.LogInformation($"Disposing {nameof(RabbitMessageService)}");
        bus?.Dispose();
        bus = null;
    }
}