using System;

namespace GeeksCoreLibrary.Modules.Branches.Models;

public class BranchActionBaseModel
{
    /// <summary>
    /// Gets or sets the ID of the branch.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the new database for the branch.
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the date and time that the action should start.
    /// </summary>
    public DateTime? StartOn { get; set; } = DateTime.Now;
}