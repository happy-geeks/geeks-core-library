using System;
using System.Threading;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.MessageBroker.Services;

public interface IMessageService : IMessageSender, IMessageReceiver
{
    
}

