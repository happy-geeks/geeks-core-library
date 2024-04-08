using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using Microsoft.Extensions.Options;
using GeeksCoreLibrary.Modules.MessageBroker.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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
    public async Task SendAsync<T>(T message, string topic, CancellationToken cancellationToken = default)
    {
        if (gclSettings.MessageBroker != MessageBrokers.RabbitMq)
        {
            logger.LogDebug("Send in RabbitMessageService called but message broker has not been set to RabbitMQ");
            return;
        }
        
        JObject jsonMessage = JObject.FromObject(message);
        
        bus ??= RabbitHutch.CreateBus(this.gclSettings.MessageBrokerConnectionString);

        logger.LogInformation($"Sending message via RabbitMQ message queue \"{topic}\". The message was: {message}");
        await bus.PubSub.PublishAsync(jsonMessage, config =>
        {
            config.WithTopic(topic);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReceiveAsync<T>(string topic, Func<T, CancellationToken, Task> onMessage, string subscriptionId = null, CancellationToken cancellationToken = default)
    {
        if (gclSettings.MessageBroker != MessageBrokers.RabbitMq)
        {
            logger.LogDebug("Send in RabbitMessageService called but message broker has not been set to RabbitMQ");
            return;
        }
        
        bus ??= RabbitHutch.CreateBus(this.gclSettings.MessageBrokerConnectionString);

        logger.LogInformation($"Started listening on message queue \"{topic}\"");

        await bus.PubSub.SubscribeAsync<T>(subscriptionId ?? gclSettings.MessageBrokerSubscriptionId, onMessage: onMessage, 
            configure: config => config.WithTopic(topic), cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        logger.LogInformation($"Disposing {nameof(RabbitMessageService)}");
        bus?.Dispose();
        bus = null;
    }
}