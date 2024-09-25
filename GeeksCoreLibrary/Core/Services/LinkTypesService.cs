using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace GeeksCoreLibrary.Core.Services;

/// <inheritdoc cref="ILinkTypesService"/>
public class LinkTypesService : ILinkTypesService, IScopedService
{
    private readonly IDatabaseConnection databaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="LinkTypesService"/>.
    /// </summary>
    public LinkTypesService(IDatabaseConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }

    /// <inheritdoc />
    public async Task<LinkSettingsModel> GetLinkTypeSettingsAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
    {
        if (linkType <= 0 && String.IsNullOrWhiteSpace(sourceEntityType) && String.IsNullOrWhiteSpace(destinationEntityType))
        {
            throw new ArgumentException($"You must enter a value in at least one of the following parameters: {nameof(linkType)}, {nameof(sourceEntityType)}, {nameof(destinationEntityType)}");
        }

        var whereClause = new List<string>();
        if (linkType > 0)
        {
            databaseConnection.AddParameter("type", linkType);
            whereClause.Add("type = ?type");
        }

        if (!String.IsNullOrWhiteSpace(sourceEntityType))
        {
            databaseConnection.AddParameter("sourceEntityType", sourceEntityType);
            whereClause.Add("connected_entity_type = ?sourceEntityType");
        }

        if (!String.IsNullOrWhiteSpace(destinationEntityType))
        {
            databaseConnection.AddParameter("destinationEntityType", destinationEntityType);
            whereClause.Add("destination_entity_type = ?destinationEntityType");
        }

        var query = $@"SELECT * FROM {WiserTableNames.WiserLink} WHERE {String.Join(" AND ", whereClause)}";

        var dataTable = await databaseConnection.GetAsync(query);
        return dataTable.Rows.Count == 0 ? new LinkSettingsModel() : DataRowToLinkSettingsModel(dataTable.Rows[0]);
    }

    /// <inheritdoc />
    public async Task<List<LinkSettingsModel>> GetAllLinkTypeSettingsAsync()
    {
        var allLinkSettings = new List<LinkSettingsModel>();

        var query = $@"SELECT * FROM {WiserTableNames.WiserLink} ORDER BY name";

        var dataTable = await databaseConnection.GetAsync(query);
        if (dataTable.Rows.Count == 0)
        {
            return allLinkSettings;
        }

        allLinkSettings.AddRange(dataTable.Rows.Cast<DataRow>().Select(DataRowToLinkSettingsModel));

        return allLinkSettings;
    }

    /// <inheritdoc />
    public async Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(int linkId)
    {
        return await GetLinkTypeSettingsByIdAsync(this, linkId);
    }

    /// <inheritdoc />
    public async Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(ILinkTypesService linkTypesService, int linkId)
    {
        IEnumerable<LinkSettingsModel> result = await linkTypesService.GetAllLinkTypeSettingsAsync();
        if (linkId > 0)
        {
            result = result.Where(t => t.Id == linkId);
        }

        return result.FirstOrDefault() ?? new LinkSettingsModel();
    }

    /// <inheritdoc />
    public async Task<string> GetTablePrefixForLinkAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
    {
        return await GetTablePrefixForLinkAsync(this, linkType, sourceEntityType, destinationEntityType);
    }

    /// <inheritdoc />
    public async Task<string> GetTablePrefixForLinkAsync(ILinkTypesService linkTypesService, int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
    {
        if (linkType == 0 && String.IsNullOrEmpty(sourceEntityType) && String.IsNullOrEmpty(destinationEntityType))
        {
            return "";
        }

        var linkTypeSettings = await linkTypesService.GetLinkTypeSettingsAsync(linkType, sourceEntityType, destinationEntityType);
        return linkTypesService.GetTablePrefixForLink(linkTypeSettings);
    }

    /// <inheritdoc />
    public string GetTablePrefixForLink(LinkSettingsModel linkTypeSettings)
    {
        return linkTypeSettings == null || !linkTypeSettings.UseDedicatedTable || linkTypeSettings.UseItemParentId ? "" : $"{linkTypeSettings.Type}_";
    }

    /// <summary>
    /// Converts a <see cref="DataRow"/> to a  <see cref="LinkSettingsModel"/>.
    /// </summary>
    /// <param name="dataRow"></param>
    /// <returns></returns>
    private static LinkSettingsModel DataRowToLinkSettingsModel(DataRow dataRow)
    {
        var linkSettings = new LinkSettingsModel
        {
            Id = dataRow.Field<int>("id"),
            Type = dataRow.Field<int>("type"),
            Name = dataRow.Field<string>("name"),
            DestinationEntityType = dataRow.Field<string>("destination_entity_type"),
            SourceEntityType = dataRow.Field<string>("connected_entity_type"),
            ShowInDataSelector = Convert.ToBoolean(dataRow["show_in_data_selector"]),
            ShowInTreeView = Convert.ToBoolean(dataRow["show_in_tree_view"]),
            UseItemParentId = dataRow.Table.Columns.Contains("use_item_parent_id") && Convert.ToBoolean(dataRow["use_item_parent_id"])
        };

        var relationship = dataRow.Field<string>("relationship");
        var duplication = dataRow.Field<string>("duplication");

        linkSettings.Relationship = relationship switch
        {
            "one-to-one" => LinkRelationships.OneToOne,
            "one-to-many" => LinkRelationships.OneToMany,
            "many-to-many" => LinkRelationships.ManyToMany,
            _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
        };

        linkSettings.DuplicationMethod = duplication switch
        {
            "none" => LinkDuplicationMethods.None,
            "copy-link" => LinkDuplicationMethods.CopyLink,
            "copy-item" => LinkDuplicationMethods.CopyItem,
            _ => throw new ArgumentOutOfRangeException(nameof(duplication), duplication, null)
        };

        if (dataRow.Table.Columns.Contains("use_dedicated_table"))
        {
            linkSettings.UseDedicatedTable = Convert.ToBoolean(dataRow["use_dedicated_table"]);
        }

        if (dataRow.Table.Columns.Contains("cascade_delete"))
        {
            linkSettings.CascadeDelete = Convert.ToBoolean(dataRow["cascade_delete"]);
        }

        return linkSettings;
    }
}