using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.Account.Models
{
    /// <summary>
    /// A model with all data from a user cookie.
    /// </summary>
    public class UserCookieDataModel
    {
        /// <summary>
        /// The unique cookie selector.
        /// </summary>
        public Guid Selector { get; set; }

        /// <summary>
        /// The Wiser 2.0 item ID of the user.
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        /// If the user is logged in as a sub account, the Wiser 2.0 item ID of the main user will be saved here. Otherwise it wil be the same value as <see cref="UserId"/>.
        /// </summary>
        public ulong MainUserId { get; set; }

        /// <summary>
        /// The Wiser 2.0 entity type of the user.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// The entity type of the main user. If the user is logging in as a main user, then it will be the same value as <see cref="EntityType"/>.
        /// </summary>
        public string MainUserEntityType { get; set; }

        /// <summary>
        /// The roles of the logged in user. These are the Wiser roles from the table wiser_roles.
        /// </summary>
        public List<RoleModel> Roles { get; set; }
        
        /// <summary>
        /// Any custom roles that the user might have, that are not Wiser roles from wiser_roles.
        /// </summary>
        public string CustomRole { get; set; }

        /// <summary>
        /// The date and time that the user logged in.
        /// </summary>
        public DateTime LoginDate { get; set; }

        /// <summary>
        /// The date and time that this cookie will expire.
        /// </summary>
        public DateTime Expires { get; set; }

        /// <summary>
        /// The IP address of the user, from the moment they logged in.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// The user agent string of the device that the user used during their login.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Optional: Any extra data of the user. In the system object 'Account_ExtraDataQuery' you can set a query for retrieving this extra data.
        /// </summary>
        public Dictionary<string, string> ExtraData { get; set; }
    }
}
