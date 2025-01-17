using System.Threading;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Core.Interfaces;

public interface IScopedProcessingService
{
    Task DoWorkAsync(CancellationToken stoppingToken);
}