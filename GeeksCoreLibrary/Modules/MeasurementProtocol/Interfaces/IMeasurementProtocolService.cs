using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces
{
    public interface IMeasurementProtocolService
    {
        Task BeginCheckoutEventAsync(decimal totalBasketPrice, OrderProcessSettingsModel orderProcessSettings, List<WiserItemModel> shoppingBasketLines);
    }
}
