using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Extensions;

namespace GeeksCoreLibrary.Components.Repeater.Models;

/// <summary>
/// This legacy model class is for converting JCL SimpleMenu to GCL Repeater.
/// </summary>
public class DbFieldLegacySettingsModel : CmsSettingsLegacy
{
    public string TabelNaam { get; set; }

    public string RijID { get; set; }

    public string VeldNaam { get; set; }

    public string RandomVeldNamen { get; set; }

    public string SQLQuery { get; set; }

    /// <summary>
    /// Convert FROM Legacy TO regular
    /// </summary>
    /// <returns></returns>
    public RepeaterCmsSettingsModel ToSettingsModel()
    {
        var query = SQLQuery;
        if (String.IsNullOrWhiteSpace(query))
        {
            if (String.IsNullOrWhiteSpace(VeldNaam) && !String.IsNullOrWhiteSpace(RandomVeldNamen))
            {
                var fields = RandomVeldNamen.Split(',');
                var rand = new Random();
                VeldNaam = fields[rand.Next(fields.Length)];
            }

            query = $"SELECT `{VeldNaam.ToMySqlSafeValue(false)}` AS value FROM `{TabelNaam.ToMySqlSafeValue(false)}` WHERE `id` = {RijID.ToMySqlSafeValue(true)}";
        }

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
                        ItemTemplate = "{value}"
                    }
                }
            },
            DataQuery = query,
            RemoveUnknownVariables = true,
            EvaluateIfElseInTemplates = true,
            SetSeoInformationFromFirstItem = false,

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
    public DbFieldLegacySettingsModel FromSettingModel(RepeaterCmsSettingsModel settings)
    {
        return this;
    }
}