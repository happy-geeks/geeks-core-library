using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Databases.Interfaces;

public interface IDocumentStorageService
{
    Task<WiserItemModel> StoreDocumentAsync(WiserItemModel wiserItem);
}