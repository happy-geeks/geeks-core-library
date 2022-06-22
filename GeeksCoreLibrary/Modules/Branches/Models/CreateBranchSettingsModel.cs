using System;
using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Branches.Models
{
    /// <summary>
    /// A model with settings for creating a new branch in Wiser.
    /// </summary>
    public class CreateBranchSettingsModel
    {
        /// <summary>
        /// Gets or sets the name of the branch to create.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the date and time that the branch should be created.
        /// </summary>
        public DateTime? StartOn { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the entities to copy to the new branch.
        /// </summary>
        public List<BranchEntitySettingsModel> Entities { get; set; }

        /// <summary>
        /// Gets or sets the name for the new customer in Wiser.
        /// </summary>
        public string NewCustomerName { get; set; }

        /// <summary>
        /// Gets or sets the sub domain for the new customer so users can login to that database.
        /// </summary>
        public string SubDomain { get; set; }

        /// <summary>
        /// Gets or sets the title for Wiser for the new customer.
        /// </summary>
        public string WiserTitle { get; set; }

        /// <summary>
        /// Gets or sets the new database for the new branch.
        /// </summary>
        public string DatabaseName { get; set; }
    }
}