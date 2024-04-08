using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;

namespace GeeksCoreLibrary.Modules.MessageBroker.Services;

public interface IMessageSender : IScopedService
{
    Task SendAsync<T>(T message, string topic, CancellationToken cancellationToken = default);
}