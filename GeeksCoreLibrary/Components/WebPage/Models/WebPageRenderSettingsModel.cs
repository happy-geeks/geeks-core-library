using System.ComponentModel;

namespace GeeksCoreLibrary.Components.WebPage.Models
{
    internal class WebPageRenderSettingsModel
    {
        [DefaultValue(5)]
        internal int SearchNumberOfLevels { get; }
    }
}
