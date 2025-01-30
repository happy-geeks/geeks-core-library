using System;
using System.Data;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.ItemFiles.Models;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.ItemFiles.Helpers;

public static class WiserFileHelpers
{
    /// <summary>
    /// Creates a new <see cref="WiserItemFileModel"/> with data from a <see cref="DataRow"/>.
    /// </summary>
    /// <param name="dataRow">The <see cref="DataRow"/> that is a result of a query on a wiser_itemfile table.</param>
    /// <returns>A <see cref="WiserItemFileModel"/> with the data from the <see cref="DataRow"/>.</returns>
    public static WiserItemFileModel DataRowToItemFile(DataRow dataRow)
    {
        if (dataRow == null)
        {
            return null;
        }

        return new WiserItemFileModel
        {
            Id = dataRow.ConvertValueIfColumnExists<ulong>("id"),
            AddedBy = dataRow.GetValueIfColumnExists<string>("added_by"),
            AddedOn = dataRow.GetValueIfColumnExists<DateTime>("added_on"),
            ItemId = dataRow.ConvertValueIfColumnExists<ulong>("item_id"),
            ItemLinkId = dataRow.ConvertValueIfColumnExists<ulong>("itemlink_id"),
            ContentType = dataRow.GetValueIfColumnExists<string>("content_type"),
            Content = dataRow.GetValueIfColumnExists<byte[]>("content"),
            ContentUrl = dataRow.GetValueIfColumnExists<string>("content_url"),
            FileName = dataRow.GetValueIfColumnExists<string>("file_name"),
            Extension = dataRow.GetValueIfColumnExists<string>("extension"),
            Title = dataRow.GetValueIfColumnExists<string>("title"),
            PropertyName = dataRow.GetValueIfColumnExists<string>("property_name"),
            Protected = dataRow.ConvertValueIfColumnExists<bool>("protected"),
            ExtraData = !dataRow.Table.Columns.Contains("extra_data") || dataRow.IsNull("extra_data")
                ? null
                : JsonConvert.DeserializeObject<WiserItemFileExtraDataModel>(dataRow.Field<string>("extra_data")!)
        };
    }
}