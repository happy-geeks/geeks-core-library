using System;
using System.Threading;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.MessageBroker.Services;

public interface IMessageService
{
    Task SendAsync<T>(string queue, T message, CancellationToken cancellationToken = default);
    
    Task ReceiveAsync<T>(string queue, Func<T, Task> onMessage, CancellationToken cancellationToken = default);
}