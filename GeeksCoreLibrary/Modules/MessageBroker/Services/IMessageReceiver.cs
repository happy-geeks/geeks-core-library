using System;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

namespace GeeksCoreLibrary.Modules.MessageBroker.Services;

public interface IMessageReceiver : ISingletonService
{
    Task ReceiveAsync<T>(string queue, Func<T, Task> onMessage, CancellationToken cancellationToken = default);
}