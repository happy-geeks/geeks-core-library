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

    /// <summary>
    /// Gets or sets whether the current settings should be saved as a template.
    /// A template is a set of default settings that can be loaded when creating or merging a branch, so that the user doesn't have to select the same things every time.
    /// A template can also be used for automatic deployments.
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Gets or sets the name of the template.
    /// Only applicable if <see cref="IsTemplate"/> is true.
    /// </summary>
    public string TemplateName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the template. This is needed when updating a template.
    /// </summary>
    public int TemplateId { get; set; }
}