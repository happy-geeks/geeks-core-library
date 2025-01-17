using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Core.Interfaces;

public interface IGeoLocationService
{
    Task<AddressInfoModel> GetAddressInfoAsync(string zipCode, string houseNumber, string houseNumberAddition = "", string country = "");
}