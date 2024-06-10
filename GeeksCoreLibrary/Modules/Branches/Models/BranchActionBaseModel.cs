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
    /// Gets or sets the host of the database.
    /// </summary>
    public string DatabaseHost { get; set; }

    /// <summary>
    /// Gets or sets the port of the database.
    /// </summary>
    public int? DatabasePort { get; set; }

    /// <summary>
    /// Gets or sets the username of the database.
    /// </summary>
    public string DatabaseUsername { get; set; }

    /// <summary>
    /// Gets or sets the password of the database.
    /// </summary>
    public string DatabasePassword { get; set; }

    /// <summary>
    /// Gets or sets the date and time that the action should start.
    /// </summary>
    public DateTime? StartOn { get; set; } = DateTime.Now;
}