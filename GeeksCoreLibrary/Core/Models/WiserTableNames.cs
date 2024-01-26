using System.Collections.Generic;

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
        /// This table contains updates for updating parents items through the wts
        /// </summary>
        public const string WiserParentUpdates = "wiser_parent_updates";
        /// <summary>
        /// This table contains dynamic queries that can be used in certain places in Wiser, such as grids and action buttons.
        /// </summary>
        public const string WiserQuery = "wiser_query";
        /// <summary>
        /// This table contains dynamic style outputs that can be used as alternative endpoint outputs
        /// </summary>
        public const string WiserStyledOutput = "wiser_styled_output";
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
        /// This table contains all login attempts that are done in Wiser for this customer.
        /// </summary>
        public const string WiserLoginAttempts = "wiser_login_attempts";
        /// <summary>
        /// This table contains periodic communications. The WTS will handle these and create new rows in <see cref="WiserCommunicationGenerated"/> when needed.
        /// </summary>
        public const string WiserCommunication = "wiser_communication";
        /// <summary>
        /// This table contains all communication that has been sent or that still needs to be sent.
        /// </summary>
        public const string WiserCommunicationGenerated = "wiser_communication_generated";
        /// <summary>
        /// This table contains all imports that are done via the import module.
        /// The WTS executes these imports.
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
        /// This table is used to store external files for the templates module.
        /// </summary>
        public const string WiserTemplateExternalFiles = "wiser_template_external_files";
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
        /// This table is used for logging who published which dynamic component at which date.
        /// </summary>
        public const string WiserDynamicContentPublishLog = "wiser_dynamic_content_publish_log";
        /// <summary>
        /// This table is used to store preview profiles for the templates module.
        /// </summary>
        public const string WiserPreviewProfiles = "wiser_preview_profiles";
        /// <summary>
        /// This table is used to store the commit history of the version control module.
        /// </summary>
        public const string WiserCommit = "wiser_commit";
        /// <summary>
        /// This table is used to store the commit history of templates of the version control module.
        /// </summary>
        public const string WiserCommitTemplate = "wiser_commit_template";
        /// <summary>
        /// This table is used to store the commit history of dynamic components of the version control module.
        /// </summary>
        public const string WiserCommitDynamicContent = "wiser_commit_dynamic_content";
        /// <summary>
        /// This table is used to store reviews about commits. So that people can request and do code reviews for commits from the template module.
        /// </summary>
        public const string WiserCommitReviews = "wiser_commit_reviews";
        /// <summary>
        /// This table is for saving the user IDs of the users that are requested to do a code review.
        /// </summary>
        public const string WiserCommitReviewRequests = "wiser_commit_review_requests";
        /// <summary>
        /// This table is used to store comments that are placed in a code review for the template module.
        /// </summary>
        public const string WiserCommitReviewComments = "wiser_commit_review_comments";
        /// <summary>
        /// This table is used by the WTS to write logs to.
        /// </summary>
        public const string WtsLogs = "wts_logs";
        /// <summary>
        /// This table is used by the WTS to store information about the services it is handling.
        /// </summary>
        public const string WtsServices = "wts_services";
        /// <summary>
        /// This table is used by Wiser for the functionality of creating branches for customers and synchronising changes from another branch to the main/original branch,
        /// When a new item has been added in another branch and it gets synchronised to production, it will most likely get a different ID in the main branch.
        /// We need this table to remember/map these IDs, so that we can also synchronise any other changes to the correct item.
        /// </summary>
        public const string WiserIdMappings = "wiser_id_mappings";
        /// <summary>
        /// This table is used by Wiser to queue things for branches, such as creating a new branch or synchronising changes from one branch to the main branch.
        /// The WTS will then handle this queue and do the actual work.
        /// </summary>
        public const string WiserBranchesQueue = "wiser_branches_queue";
        /// <summary>
        /// This table is used by Wiser to temporarily store data with various Wiser statistics.
        /// </summary>
        public const string WiserDashboard = "wiser_dashboard";
        /// <summary>
        /// This table is used by Wiser to track how many times users log in, and for how long they remain active.
        /// </summary>
        public const string WiserLoginLog = "wiser_login_log";
        /// <summary>
        /// This table is used for keeping track of how when components are rendered and how long it takes to render them.
        /// This is then used in Wiser to show information about the performance of each component.
        /// </summary>
        public const string WiserDynamicContentRenderLog = "wiser_dynamic_content_render_log";
        /// <summary>
        /// This table is used for keeping track of how when templates are rendered and how long it takes to render them.
        /// This is then used in Wiser to show information about the performance of each template.
        /// </summary>
        public const string WiserTemplateRenderLog = "wiser_template_render_log";
        /// <summary>
        /// This table is used by the RequestLoggingMiddleware to log all incoming requests, if enabled.
        /// </summary>
        public const string GclRequestLog = "gcl_request_log";
        /// <summary>
        /// All deleted items will be moved to archive tables, this is the suffix for those archive tables.
        /// </summary>
        public const string ArchiveSuffix = "_archive";
        /// <summary>
        /// All tables that also have an archive.
        /// </summary>
        public static readonly List<string> TablesWithArchive = new()
        {
            WiserItem,
            WiserItemDetail,
            WiserItemFile,
            WiserItemLink,
            WiserItemLinkDetail
        };
        /// <summary>
        /// All tables that can have a dedicated version for certain entity types, such as "basket_wiser_item".
        /// </summary>
        public static readonly List<string> TablesThatCanHaveEntityPrefix = new()
        {
            WiserItem,
            WiserItemDetail,
            WiserItemFile
        };
        /// <summary>
        /// All tables that can have a dedicated version for certain link types, such as "1234_wiser_itemlink".
        /// </summary>
        public static readonly List<string> TablesThatCanHaveLinkPrefix = new()
        {
            WiserItemLink,
            WiserItemLinkDetail,
            WiserItemFile
        };
    }
}