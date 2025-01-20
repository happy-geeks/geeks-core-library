using System.Data.Common;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Objects.Models;

namespace GeeksCoreLibrary.Modules.Objects.Extensions;

public static class DbDataReaderExtensions
{
    public static SettingObject ToObjectModel(this DbDataReader reader)
    {
        return new SettingObject
        {
            Key = reader.GetStringHandleNull("key"),
            Value = reader.GetStringHandleNull("value"),
            Description = reader.GetStringHandleNull("description"),
            TypeNumber = reader.GetInt32(reader.GetOrdinal("typenr"))
        };
    }
}