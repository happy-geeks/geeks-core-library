﻿using System;

namespace GeeksCoreLibrary.Modules.Templates.Models;

public class PageResourceModel
{
    /// <summary>
    /// Gets or sets the ID of the resource.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the full (absolute) path to the file.
    /// </summary>
    public Uri Uri { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the file.
    /// This will be added to the integrity attribute of the script or link tag.
    /// </summary>
    public string Hash { get; set; }

    /// <summary>
    /// Gets or sets the ordering number, to decide in which other the resources should be loaded.
    /// </summary>
    public int Ordering { get; set; }
}