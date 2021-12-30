using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.WebPage.Interfaces
{
    public interface IWebPagesService
    {
        Task<(ulong Id, string Title, string FixedUrl, List<string> Path, List<ulong> Parents)?> GetWebPageViaFixedUrl(string fixedUrl);
    }
}
