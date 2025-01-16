using GeeksCoreLibrary.Core.Cms;

namespace GeeksCoreLibrary.Components.WebPage.Models;

public class WebPageLegacySettingsModel : CmsSettingsLegacy
{
    public bool SetSeoInfo { get; set; }

    public string PageName { get; set; }

    public ulong PageId { get; set; }

    public string PathMustContainName { get; set; }

    public int SearchNumberOfLevels { get; set; }

    /// <summary>
    /// Convert FROM Legacy TO regular
    /// </summary>
    public WebPageCmsSettingsModel ToSettingsModel()
    {
        return new WebPageCmsSettingsModel
        {
            Description = VisibleDescription,
            HandleRequest = HandleRequest,
            UserNeedsToBeLoggedIn = UserNeedsToBeLoggedIn,
            SetSeoInfo = SetSeoInfo,
            ReturnNotFoundStatusCodeOnNoData = Return404OnNoData,
            PageName = PageName,
            PageId = PageId,
            PathMustContainName = PathMustContainName,
            SearchNumberOfLevels = SearchNumberOfLevels
        };
    }
}