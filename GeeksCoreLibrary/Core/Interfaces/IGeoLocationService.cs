using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Core.Interfaces
{
    public interface IGeoLocationService
    {
        Task<AddressInfoModel> GetAddressInfo(string zipCode, string houseNumber, string houseNumberAddition = "", string country = "");
    }
}
