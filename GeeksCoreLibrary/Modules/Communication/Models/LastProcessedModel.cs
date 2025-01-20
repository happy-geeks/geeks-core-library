using System;
using GeeksCoreLibrary.Modules.Communication.Enums;

namespace GeeksCoreLibrary.Modules.Communication.Models;

/// <summary>
/// A model for keeping track of when each communication has been processed last.
/// </summary>
public class LastProcessedModel
{
    /// <summary>
    /// Gets or sets the type of communication (ie E-mail, SMS etc).
    /// </summary>
    public CommunicationTypes Type { get; set; }

    /// <summary>
    /// Gets or sets the date and time that this communication has been processed last.
    /// </summary>
    public DateTime DateTime { get; set; }
}