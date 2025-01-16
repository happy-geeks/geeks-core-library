using System.Collections.Generic;
using GeeksCoreLibrary.Core.Cms;

namespace GeeksCoreLibrary.Components.Repeater.Models;

/// <summary>
/// This legacy model class is for converting JCL ProductModule to GCL Repeater.
/// </summary>
public class ProductModuleLegacySettingsModel : CmsSettingsLegacy
{
    public string Header { get; set; }

    public string Footer { get; set; }

    public string ItemHTML { get; set; }

    public string TussenItemHTML { get; set; }

    public bool PlaceProductBanners { get; set; }

    public string ProductBannerTemplate { get; set; }

    public int GroepeerItemsPer { get; set; }

    public string Groupheader { get; set; }

    public bool ShowHeaderOnFirst { get; set; }

    public string GroupFooter { get; set; }

    public bool ShowFooterOnLast { get; set; }

    public string EmptyItemHTML { get; set; }

    public uint AantalItemsOpPagina { get; set; }

    /// <summary>
    /// Convert FROM Legacy TO regular
    /// </summary>
    /// <returns></returns>
    public RepeaterCmsSettingsModel ToSettingsModel()
    {
        // Do conversion
        return new()
        {
            ComponentMode = Repeater.ComponentModes.Repeater,
            Description = VisibleDescription,
            GroupingTemplates = new SortedList<string, RepeaterTemplateModel>
            {
                {
                    "",
                    new RepeaterTemplateModel
                    {
                        HeaderTemplate = Header,
                        FooterTemplate = Footer,
                        ItemTemplate = ItemHTML,
                        BetweenItemsTemplate = TussenItemHTML
                    }
                }
            },
            DataQuery = SQLQuery,
            RemoveUnknownVariables = true,
            EvaluateIfElseInTemplates = true,
            PlaceProductBanners = PlaceProductBanners,
            ProductBannerTemplate = ProductBannerTemplate,
            CreateGroupsOfNItems = GroepeerItemsPer,
            GroupHeader = Groupheader,
            ShowGroupHeaderForFirstGroup = ShowHeaderOnFirst,
            GroupFooter = GroupFooter,
            ShowGroupFooterForLastGroup = ShowFooterOnLast,
            EmptyGroupItemHtml = EmptyItemHTML,
            ItemsPerPage = AantalItemsOpPagina,

            // The JCL ProductModule always shows header and footer if there's no data. It doesn't have a property for it.
            ShowBaseHeaderAndFooterOnNoData = true,

            // Inherited items from abstract parent
            UserNeedsToBeLoggedIn = UserNeedsToBeLoggedIn,
            HandleRequest = HandleRequest,

            // Tell the Repeater to build the HTML like the JCL used to do, instead of the new and more logical way.
            LegacyMode = true
        };
    }

    /// <summary>
    /// Convert FROM regular TO legacy
    /// </summary>
    /// <returns></returns>
    public ProductModuleLegacySettingsModel FromSettingModel(RepeaterCmsSettingsModel settings)
    {
        return this;
    }
}