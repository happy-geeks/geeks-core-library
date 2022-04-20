using System.Collections.Generic;
using GeeksCoreLibrary.Core.Cms;

namespace GeeksCoreLibrary.Components.Repeater.Models
{
    /// <summary>
    /// This legacy model class is for converting JCL SimpleMenu to GCL Repeater.
    /// </summary>
    public class SimpleMenuLegacySettingsModel : CmsSettingsLegacy
    {
        public bool AddFilterSelectionQueryPart { get; set; }

        public string Between2ItemsTemplate { get; set; }

        public bool DoReplacementsOnHeaderndFooter { get; set; }

        public string FilePath { get; set; }

        public string FooterTemplate { get; set; }

        public string FunctionCallAfterApiCall { get; set; }

        public string HeaderTemplate { get; set; }

        public string ItemTemplate { get; set; }

        public string JsonGroupSelector { get; set; }

        public string JSONHeaders { get; set; }

        public string JSONPayload { get; set; }

        public string JsonRequestMethod { get; set; }

        public string JsonURLorString { get; set; }

        public string NoDataTemplate { get; set; }

        public string RedirectUrlAfterFailure { get; set; }

        public string RedirectUrlAfterSuccess { get; set; }

        public bool ReplaceUnknownVariablesWithEmptyString { get; set; }

        public string SelectedField { get; set; }

        public string SelectedItemTemplate { get; set; }

        public string SelectionKey { get; set; }

        public string SelectionSource { get; set; }

        public bool SetSEOInfoFromFirstItem { get; set; }

        public bool UseTestValuesIfEmpty { get; set; }

        public int WiserModuleId { get; set; }

        public int WiserParentItemId { get; set; }

        public int WiserPathMustContainID { get; set; }

        public string WiserPathMustContainName { get; set; }

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
                            HeaderTemplate = HeaderTemplate,
                            FooterTemplate = FooterTemplate,
                            ItemTemplate = ItemTemplate,
                            BetweenItemsTemplate = Between2ItemsTemplate,
                            NoDataTemplate = NoDataTemplate,
                            SelectedItemTemplate = SelectedItemTemplate
                        }
                    }
                },
                DataQuery = SQLQuery,
                RemoveUnknownVariables = ReplaceUnknownVariablesWithEmptyString,
                EvaluateIfElseInTemplates = true,
                SetSeoInformationFromFirstItem = SetSEOInfoFromFirstItem,

                // The JCL SimpleMenu never shows header and footer if there's no data. It doesn't have a property for it.
                ShowBaseHeaderAndFooterOnNoData = false,

                // Inherited items from abstract parent
                UserNeedsToBeLoggedIn = UserNeedsToBeLoggedIn,
                HandleRequest = HandleRequest,
                Return404OnNoData = Return404OnNoData
            };
        }

        /// <summary>
        /// Convert FROM regular TO legacy
        /// </summary>
        /// <returns></returns>
        public SimpleMenuLegacySettingsModel FromSettingModel(RepeaterCmsSettingsModel settings)
        {
            return this;
        }
    }
}
