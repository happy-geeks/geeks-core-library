using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    public class OrderProcessCmsSettingsModel : CmsSettings
    {
        public OrderProcess.ComponentModes ComponentMode { get; set; } = OrderProcess.ComponentModes.Automatic;
        
        #region Tab DataSource properties

        [CmsProperty(
            PrettyName = "Order process item ID",
            Description = "The Wiser item ID of the order process that should be retrieved.",
            DeveloperRemarks = "",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10
        )]
        public ulong OrderProcessId { get; set; }

        #endregion
    }
}
