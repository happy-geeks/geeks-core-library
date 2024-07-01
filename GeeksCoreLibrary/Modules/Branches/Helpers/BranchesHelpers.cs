using System;
using System.Collections.Generic;
using System.Linq;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Models;

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
                tablePrefix = tableName.Replace(WiserTableNames.WiserItem, "", StringComparison.OrdinalIgnoreCase);
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemDetail, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.Replace(WiserTableNames.WiserItemDetail, "", StringComparison.OrdinalIgnoreCase);
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemFile, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.Replace(WiserTableNames.WiserItemFile, "", StringComparison.OrdinalIgnoreCase);
                if (originalItemId == 0)
                {
                    isWiserItemChange = false;
                }
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemLink, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.Replace(WiserTableNames.WiserItemLink, "", StringComparison.OrdinalIgnoreCase);
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemLinkDetail, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.Replace(WiserTableNames.WiserItemLinkDetail, "", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isWiserItemChange = false;
            }

            return (tablePrefix, isWiserItemChange);
        }

        /// <summary>
        /// This method is used to keep track of objects that have been created and deleted in the branch.
        /// This function has been added to the GCL, because it's needed in both Wiser and the WTS.
        /// The purpose of this function is that if an object has both been created and also deleted in the branch,
        /// then there is no point in merging that object to the production database, it would just clutter the history.
        /// </summary>
        /// <param name="trackedObjects">The list used to keep track of these changes.</param>
        /// <param name="action">The action from wiser_history.</param>
        /// <param name="objectId">The item_id from wiser_history.</param>
        /// <param name="tableName">The table_name from wiser_history.</param>
        public static void TrackObjectAction(List<ObjectCreatedInBranchModel> trackedObjects, string action, string objectId, string tableName)
        {
            var wiserObject = trackedObjects.FirstOrDefault(i => i.ObjectId == objectId && String.Equals(i.TableName, tableName, StringComparison.OrdinalIgnoreCase));
            switch (action)
            {
                case "CREATE_ITEM":
                case "ADD_LINK":
                case "ADD_FILE":
                case "INSERT_ENTITY":
                case "INSERT_ENTITYPROPERTY":
                case "INSERT_QUERY":
                case "INSERT_MODULE":
                case "INSERT_DATA_SELECTOR":
                case "INSERT_PERMISSION":
                case "INSERT_USER_ROLE":
                case "INSERT_FIELD_TEMPLATE":
                case "INSERT_LINK_SETTING":
                case "INSERT_API_CONNECTION":
                case "INSERT_ROLE":
                    if (wiserObject == null)
                    {
                        trackedObjects.Add(new ObjectCreatedInBranchModel {ObjectId = objectId, TableName = tableName});
                    }

                    break;
                case "DELETE_ITEM":
                case "REMOVE_LINK":
                case "DELETE_FILE":
                case "DELETE_ENTITY":
                case "DELETE_ENTITYPROPERTY":
                case "DELETE_QUERY":
                case "DELETE_DATA_SELECTOR":
                case "DELETE_MODULE":
                case "DELETE_PERMISSION":
                case "DELETE_USER_ROLE":
                case "DELETE_FIELD_TEMPLATE":
                case "DELETE_LINK_SETTING":
                case "DELETE_API_CONNECTION":
                case "DELETE_ROLE":
                    if (wiserObject != null)
                    {
                        wiserObject.AlsoDeleted = true;
                    }

                    break;
                case "UNDELETE_ITEM":
                    if (wiserObject != null)
                    {
                        wiserObject.AlsoUndeleted = true;
                    }

                    break;
            }
        }
    }
}