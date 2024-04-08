using System;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

namespace GeeksCoreLibrary.Modules.MessageBroker.Services;

public interface IMessageReceiver : ISingletonService
{
    Task ReceiveAsync<T>(string topic, Func<T, CancellationToken, Task> onMessage, string subscriptionId = null,
        CancellationToken cancellationToken = default);
}