using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace GeeksCoreLibrary.Core.Services;

/// <inheritdoc cref="IRolesService" />
public class RolesService : IRolesService, ITransientService
{
    private readonly IDatabaseConnection databaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="RolesService"/>.
    /// </summary>
    public RolesService(IDatabaseConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }

    /// <inheritdoc />
    public async Task<List<RoleModel>> GetRolesAsync(bool includePermissions = false)
    {
        string query;
        if (includePermissions)
        {
            query = $@"SELECT 
    role.id, 
    role.role_name,
    permission.item_id,
    permission.entity_property_id,
    permission.module_id,
    permission.permissions,
	permission.endpoint_url,
	permission.endpoint_http_method
FROM {WiserTableNames.WiserRoles} AS role 
LEFT JOIN {WiserTableNames.WiserPermission} AS permission ON permission.role_id = role.id
ORDER BY role_name ASC";
        }
        else
        {
            query = $@"SELECT
    id,
    role_name
FROM {WiserTableNames.WiserRoles}
ORDER BY role_name ASC";
        }

        var dataTable = await databaseConnection.GetAsync(query);

        var results = new List<RoleModel>();
        foreach (DataRow dataRow in dataTable.Rows)
        {
            var roleId = dataRow.Field<int>("id");
            var role = results.SingleOrDefault(r => r.Id == roleId);
            if (role == null)
            {
                role = new RoleModel
                {
                    Id = roleId,
                    Name = dataRow.Field<string>("role_name")
                };

                results.Add(role);
            }

            if (!includePermissions)
            {
                continue;
            }

            role.Permissions ??= new List<PermissionModel>();
            role.Permissions.Add(new PermissionModel
            {
                ItemId = Convert.ToUInt64(dataRow["item_id"]),
                ModuleId = dataRow.Field<int>("module_id"),
                EntityPropertyId = dataRow.Field<int>("entity_property_id"),
                Permissions = (AccessRights)dataRow.Field<int>("permissions"),
                EndpointUrl = dataRow.Field<string>("endpoint_url"),
                EndpointHttpMethod = new HttpMethod(dataRow.Field<string>("endpoint_http_method"))
            });
        }

        return results;
    }
}