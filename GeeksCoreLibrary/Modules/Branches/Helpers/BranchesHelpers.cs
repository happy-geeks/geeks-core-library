using System;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Branches.Helpers
{
    public static class BranchesHelpers
    {
        /// <summary>
        /// Get the prefix for a wiser item table.
        /// </summary>
        /// <param name="tableName">The full name of the table.</param>
        /// <param name="originalItemId">The original item ID.</param>
        /// <returns>The table prefix and whether or not this is something connected to an item from [prefix]wiser_item.</returns>
        public static (string TablePrefix, bool IsWiserItemChange) GetTablePrefix(string tableName, ulong originalItemId)
        {
            var isWiserItemChange = true;
            var tablePrefix = "";
            if (tableName.EndsWith(WiserTableNames.WiserItem, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItem, "");
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemDetail, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemDetail, "");
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemFile, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemFile, "");
                if (originalItemId == 0)
                {
                    isWiserItemChange = false;
                }
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemLink, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemLink, "");
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemLinkDetail, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemLinkDetail, "");
            }
            else
            {
                isWiserItemChange = false;
            }

            return (tablePrefix, isWiserItemChange);
        }
    }
}