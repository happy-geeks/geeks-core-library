using System;

namespace GeeksCoreLibrary.Modules.WiserDashboard.Models;

/// <summary>
/// A model for a WTS service.
/// </summary>
public class Service
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the configuration the service is in.
    /// </summary>
    public string Configuration { get; set; }

    /// <summary>
    /// Gets or sets the time ID of the service.
    /// </summary>
    public int TimeId { get; set; }

    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// Gets or sets the scheme type of the service.
    /// </summary>
    public string Scheme { get; set; }

    /// <summary>
    /// Gets or sets the last time the service has run.
    /// </summary>
    public DateTime? LastRun { get; set; }

    /// <summary>
    /// Gets or sets the next time the service will run.
    /// </summary>
    public DateTime? NextRun { get; set; }

    /// <summary>
    /// Gets or sets the time in minutes the last run needed to finish.
    /// </summary>
    public double RunTime { get; set; }

    /// <summary>
    /// Gets or sets the state of the service.
    /// </summary>
    public string State { get; set; }

    /// <summary>
    /// Gets or sets if the service is paused.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Gets or sets if the service need to be run an extra time.
    /// </summary>
    public bool ExtraRun { get; set; }
}