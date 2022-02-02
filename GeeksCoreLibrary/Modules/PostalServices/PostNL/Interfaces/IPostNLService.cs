using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Services;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Interfaces
{
    public interface IPostNLService
    {
        /// <summary>
        /// Creates a PostNL track and trace label using the specified order and PostNL API request
        /// </summary>
        /// <param name="orderId">The orderId of the order a label must be created for</param>
        /// <param name="request">The API request response</param>
        /// <returns>Object containing all the information about the order</returns>
        public Task<ShipmentResponseModel> CreateTrackTraceLabel(string orderId, ShipmentRequestModel request);

        /// <summary>
        /// Creates a new barcode using the PostNL api
        /// </summary>
        /// <param name="orderId">OrderId of the order a barcode must be created for</param>
        /// <param name="shippingLocation">The location the package of the order will be send to</param>
        /// <returns>Model containing the information about the created barcode</returns>
        public Task<BarcodeResponseModel> CreateNewBarcode(string orderId, PostNLService.ShippingLocations shippingLocation = PostNLService.ShippingLocations.Netherlands);

        /// <summary>
        /// Gets the settings of a specified shipping location
        /// </summary>
        /// <param name="shippingLocation">The shipping location the order must be send to</param>
        /// <returns>Model of settings of the specified shipping location</returns>
        public Task<SettingsModel> GetSettings(PostNLService.ShippingLocations shippingLocation);
    }
}
