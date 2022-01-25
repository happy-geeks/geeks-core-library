namespace GeeksCoreLibrary.Core.Models
{
    public class WiserTableNames
    {
        /// <summary>
        /// This table contains all Wiser items.
        /// This can be products, orders, customers etc.
        /// </summary>
        public const string WiserItem = "wiser_item";
        /// <summary>
        /// This table contains all details/properties for all items.
        /// </summary>
        public const string WiserItemDetail = "wiser_itemdetail";
        /// <summary>
        /// This table contains all connections between items.
        /// For example, an order can contain order lines. The order lines are then connected to the order via this table.
        /// </summary>
        public const string WiserItemLink = "wiser_itemlink";
        /// <summary>
        /// This table contains possible links between items and is used for things like the data selector, to show the user what links they can use.
        /// </summary>
        public const string WiserLink = "wiser_link";
        /// <summary>
        /// This table contains all item link details. These are keys and values that are dependent on a link between 2 items.
        /// </summary>
        public const string WiserItemLinkDetail = "wiser_itemlinkdetail";
        /// <summary>
        /// This table contains all files. They can be connected to an item or an item link.
        /// </summary>
        public const string WiserItemFile = "wiser_itemfile";
        /// <summary>
        /// This table contains all entity types and can be used to configure them.
        /// </summary>
        public const string WiserEntity = "wiser_entity";
        /// <summary>
        /// This table contains all fields per entity type. These are the fields that are shown in Wiser when you open an item there.
        /// </summary>
        public const string WiserEntityProperty = "wiser_entityproperty";
        /// <summary>
        /// This table contains settings for all Wiser modules.
        /// </summary>
        public const string WiserModule = "wiser_module";
        /// <summary>
        /// This table contains dynamic queries that can be used in certain places in Wiser, such as grids and action buttons.
        /// </summary>
        public const string WiserQuery = "wiser_query";
        /// <summary>
        /// This table contains the history of everything. Any change to an item or link will be saved here.
        /// </summary>
        public const string WiserHistory = "wiser_history";
        /// <summary>
        /// This table contains the default HTML en javascript for all fields in Wiser.
        /// </summary>
        public const string WiserFieldTemplates = "wiser_field_templates";
        /// <summary>
        /// This table contains all links between users and roles. This can be used for users that login Wiser and for users that login to the website.
        /// A user can have multiple roles and a role can have multiple users.
        /// </summary>
        public const string WiserUserRoles = "wiser_user_roles";
        /// <summary>
        /// This table contains all roles that users can have.
        /// A user can have multiple roles and a role can have multiple users.
        /// </summary>
        public const string WiserRoles = "wiser_roles";
        /// <summary>
        /// This table contains all permissions for roles.
        /// </summary>
        public const string WiserPermission = "wiser_permission";
        /// <summary>
        /// This table contains settings for connections with external APIs, that can be used in Wiser.
        /// </summary>
        public const string WiserApiConnection = "wiser_api_connection";
        /// <summary>
        /// This table contains authorization tokens for users that can login to Wiser.
        /// There is a different table (gcl_user_auth_token) for users that login to the website.
        /// </summary>
        public const string WiserUsersAuthenticationTokens = "wiser_user_auth_token";
        /// <summary>
        /// This table contains custom sort orders for users. Users can sort modules in wiser or fields on an entity type, that will all be saved here.
        /// </summary>
        public const string WiserOrdering = "wiser_ordering";
        /// <summary>
        /// This table contains all login attempts that are done in Wiser for this customer.
        /// </summary>
        public const string WiserLoginAttempts = "wiser_login_attempts";
        /// <summary>
        /// This table contains periodic communications. The AIS will handle these and create new rows in <see cref="WiserCommunicationGenerated"/> when needed.
        /// </summary>
        public const string WiserCommunication = "wiser_communication";
        /// <summary>
        /// This table contains all communication that has been sent or that still needs to be sent.
        /// </summary>
        public const string WiserCommunicationGenerated = "wiser_communication_generated";
        /// <summary>
        /// This table contains all imports that are done via the import module.
        /// The AIS executes these imports.
        /// </summary>
        public const string WiserImport = "wiser_import";
        /// <summary>
        /// This table contains all logs for imports done via the import module.
        /// </summary>
        public const string WiserImportLog = "wiser_import_log";
        /// <summary>
        /// This table contains all data selectors for the data selector module.
        /// </summary>
        public const string WiserDataSelector = "wiser_data_selector";
        /// <summary>
        /// This table contains grant data for IdentityServer4, such as refresh tokens, so that they don't expire when the application pool gets recycled.
        /// </summary>
        public const string WiserGrantStore = "wiser_grant_store";
        /// <summary>
        /// This table is used to keep track of when all other table definitions have been updated last. Wiser can then use this to make sure table definitions for all customers will be automatically kept up-to-date.
        /// </summary>
        public const string WiserTableChanges = "wiser_table_changes";
        /// <summary>
        /// This table is used to store templates from the templates module.
        /// </summary>
        public const string WiserTemplate = "wiser_template";
        /// <summary>
        /// This table is used to store dynamic content components for the templates module.
        /// </summary>
        public const string WiserDynamicContent = "wiser_dynamic_content";
        /// <summary>
        /// This table is used to link one or more dynamic content to one or more templates.
        /// </summary>
        public const string WiserTemplateDynamicContent = "wiser_template_dynamic_content";
        /// <summary>
        /// This table is used for logging who published which template at which date.
        /// </summary>
        public const string WiserTemplatePublishLog = "wiser_template_publish_log";
        /// <summary>
        /// This table is used to link multiple templates from the templates module to each other.
        /// </summary>
        public const string WiserTemplateLink = "wiser_template_link";
        /// <summary>
        /// This table is used to store preview profiles for the templates module.
        /// </summary>
        public const string WiserPreviewProfiles = "wiser_preview_profiles";
        /// <summary>
        /// All deleted items will be moved to archive tables, this is the suffix for those archive tables.
        /// </summary>
        public const string ArchiveSuffix = "_archive";
    }
}
