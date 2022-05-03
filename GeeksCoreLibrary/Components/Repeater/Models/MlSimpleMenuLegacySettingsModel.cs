using System.Collections.Generic;
using GeeksCoreLibrary.Core.Cms;

namespace GeeksCoreLibrary.Components.Repeater.Models
{
    /// <summary>
    /// This legacy model class is for converting JCL MLSimpleMenu to GCL Repeater.
    /// </summary>
    public class MlSimpleMenuLegacySettingsModel : CmsSettingsLegacy
    {
        public int AantalLevels { get; set; } 
        
        public bool AddFilterSelectionQueryPart { get; set; } 
        
        public bool DoEvaluateTemplates { get; set; } 
        
        public string EmptyMenuHTML { get; set; } 
        
        public string Footer { get; set; } 
        
        public string Footer1 { get; set; } 
        
        public string Footer2 { get; set; } 
        
        public string Footer3 { get; set; } 
        
        public string Footer4 { get; set; } 
        
        public string Footer5 { get; set; } 
        
        public string Header { get; set; } 
        
        public string Header1 { get; set; } 
        
        public string Header2 { get; set; } 
        
        public string Header3 { get; set; } 
        
        public string Header4 { get; set; } 
        
        public string Header5 { get; set; } 
        
        public string ItemTemplate1 { get; set; } 
        
        public string ItemTemplate2 { get; set; } 
        
        public string ItemTemplate3 { get; set; } 
        
        public string ItemTemplate4 { get; set; } 
        
        public string ItemTemplate5 { get; set; } 
        
        public bool NestedLayers { get; set; } 
        
        public bool ReplaceUnknownVariablesWithEmptyString { get; set; } 
        
        public string SelectedItemTemplate1 { get; set; } 
        
        public string SelectedItemTemplate2 { get; set; } 
        
        public string SelectedItemTemplate3 { get; set; } 
        
        public string SelectedItemTemplate4 { get; set; } 
        
        public string SelectedItemTemplate5 { get; set; } 
        
        public string TussenTemplate1 { get; set; } 
        
        public string TussenTemplate2 { get; set; } 
        
        public string TussenTemplate3 { get; set; } 
        
        public string TussenTemplate4 { get; set; } 
        
        public string TussenTemplate5 { get; set; }

        /// <summary>
        /// Convert FROM Legacy TO regular
        /// </summary>
        /// <returns></returns>
        public RepeaterCmsSettingsModel ToSettingsModel()
        {
            // Do conversion
            var repeaterCmsSettingsModel = new RepeaterCmsSettingsModel
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
                            NoDataTemplate = EmptyMenuHTML
                        }
                    }
                },
                DataQuery = SQLQuery,
                RemoveUnknownVariables = ReplaceUnknownVariablesWithEmptyString,
                EvaluateIfElseInTemplates = DoEvaluateTemplates,

                // The JCL MLSimpleMenu never shows header and footer if there's no data. It doesn't have a property for it.
                ShowBaseHeaderAndFooterOnNoData = false,

                // Inherited items from abstract parent
                UserNeedsToBeLoggedIn = UserNeedsToBeLoggedIn,
                HandleRequest = HandleRequest,

                // Tell the Repeater to build the HTML like the JCL used to do, instead of the new and more logical way.
                LegacyMode = true
            };

            for (var i = 1; i <= AantalLevels; i++)
            {
                // Tip: Use id, but maybe it is better to use the column index?
                var identifier = $"id{i}" ;
                var repeaterTemplate = new RepeaterTemplateModel
                {
                    HeaderTemplate = i switch
                    {
                        1 => Header1,
                        2 => Header2,
                        3 => Header3,
                        4 => Header4,
                        5 => Header5,
                        _ => ""
                    },
                    FooterTemplate = i switch
                    {
                        1 => Footer1,
                        2 => Footer2,
                        3 => Footer3,
                        4 => Footer4,
                        5 => Footer5,
                        _ => ""
                    },
                    ItemTemplate = i switch
                    {
                        1 => ItemTemplate1,
                        2 => ItemTemplate2,
                        3 => ItemTemplate3,
                        4 => ItemTemplate4,
                        5 => ItemTemplate5,
                        _ => ""
                    },
                    BetweenItemsTemplate = i switch
                    {
                        1 => TussenTemplate1,
                        2 => TussenTemplate2,
                        3 => TussenTemplate3,
                        4 => TussenTemplate4,
                        5 => TussenTemplate5,
                        _ => ""
                    }
                };

                repeaterCmsSettingsModel.GroupingTemplates.Add(identifier, repeaterTemplate);
            }

            return repeaterCmsSettingsModel;
        }

        /// <summary>
        /// Convert FROM regular TO legacy
        /// </summary>
        /// <returns></returns>
        public MlSimpleMenuLegacySettingsModel FromSettingModel(RepeaterCmsSettingsModel settings)
        {
            return this;
        }
    }
}
