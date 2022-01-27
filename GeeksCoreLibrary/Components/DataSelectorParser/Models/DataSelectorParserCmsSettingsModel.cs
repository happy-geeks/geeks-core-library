using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace GeeksCoreLibrary.Components.DataSelectorParser.Models
{
    public class DataSelectorParserCmsSettingsModel : CmsSettings
    {
        public DataSelectorParser.ComponentModes ComponentMode { get; set; } = DataSelectorParser.ComponentModes.Render;

        #region Tab DataSource properties

        [CmsProperty(
            PrettyName = "Data Selector ID",
            Description = "The ID of a saved data selector.",
            DeveloperRemarks = "This property takes precedence over 'Data Selector JSON'.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            DisplayOrder = 10
        )]
        public string DataSelectorId { get; set; }

        [CmsProperty(
            PrettyName = "Data Selector JSON",
            Description = "The raw request JSON of a data selector.",
            DeveloperRemarks = "The property 'Data Selector ID' takes precedence over this property.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Basic,
            TextEditorType = CmsAttributes.CmsTextEditorType.JsonEditor,
            DisplayOrder = 20
        )]
        public string DataSelectorJson { get; set; }

        [CmsProperty(
            PrettyName = "Data Selector Demo JSON",
            Description = "The raw response JSON of a data selector.",
            DeveloperRemarks = "Will only be used if both 'Data Selector ID' and 'Data Selector JSON' are empty.",
            TabName = CmsAttributes.CmsTabName.DataSource,
            GroupName = CmsAttributes.CmsGroupName.Demo,
            TextEditorType = CmsAttributes.CmsTextEditorType.JsonEditor,
            DisplayOrder = 30
        )]
        public string DataSelectorDemoJson { get; set; }

        #endregion

        #region Tab Layout properties

        [CmsProperty(
            PrettyName = "Template",
            Description = "The (HTML) template that will be used.",
            DeveloperRemarks = "This is only placed if there are items.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.HtmlEditor,
            DisplayOrder = 10
        )]
        public string Template { get; set; }

        [CmsProperty(
            PrettyName = "JavaScript Template",
            Description = "If this component requires any JavaScript, you can write that here.",
            TabName = CmsAttributes.CmsTabName.Layout,
            GroupName = CmsAttributes.CmsGroupName.Templates,
            TextEditorType = CmsAttributes.CmsTextEditorType.JsEditor,
            DisplayOrder = 20
        )]
        public string TemplateJavaScript { get; set; }

        #endregion

        #region Tab Behavior properties

        [CmsProperty(
            PrettyName = "Set SEO info from first item",
            Description = "Use the SEO Fields of the first item to set the Page SEO fields.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            DisplayOrder = 30
        )]
        public bool SetSeoInfoFromFirstItem { get; set; }

        [CmsProperty(
            PrettyName = "SEO title entity property name",
            Description = "Specifies which field in the response should be used to determine the SEO title.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextField,
            DisplayOrder = 31
        )]
        public bool SeoTitleEntityPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "SEO description entity property name",
            Description = "Specifies which field in the response should be used to determine the SEO description.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextField,
            DisplayOrder = 32
        )]
        public bool SeoDescriptionEntityPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "Canonical URL entity property name",
            Description = "Specifies which field in the response should be used to determine the canonical URL.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextField,
            DisplayOrder = 33
        )]
        public bool SeoCanoicalUrlEntityPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "No-index entity property name",
            Description = "Specifies which field in the response should be used to determine the no-index value.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextField,
            DisplayOrder = 34
        )]
        public bool SeoNoIndexEntityPropertyName { get; set; }

        [CmsProperty(
            PrettyName = "No-follow entity property name",
            Description = "Specifies which field in the response should be used to determine the no-follow value.",
            TabName = CmsAttributes.CmsTabName.Behavior,
            GroupName = CmsAttributes.CmsGroupName.Seo,
            TextEditorType = CmsAttributes.CmsTextEditorType.TextField,
            DisplayOrder = 35
        )]
        public bool SeoNoFollowEntityPropertyName { get; set; }

        #endregion
    }
}
