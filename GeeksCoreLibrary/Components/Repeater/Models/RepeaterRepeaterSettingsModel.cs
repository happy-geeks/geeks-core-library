using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Repeater.Models;

internal class RepeaterRepeaterSettingsModel
{
    [DefaultValue(true)]
    internal string BannerUsesProductBlockSpace { get; set; }
}