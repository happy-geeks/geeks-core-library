using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.OrderProcess.Interfaces
{
    public interface IOrderProcessesService
    {
        Task<(ulong Id, string Title, string FixedUrl)?> GetOrderProcessViaFixedUrl(string fixedUrl);
    }
}
