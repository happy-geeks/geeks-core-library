using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Communication.Enums;

namespace GeeksCoreLibrary.Modules.Communication.Models;

/// <summary>
/// A model for communication settings. These are settings from the table wiser_communication and these are used for sending automatic periodic communications based on certain criteria.
/// </summary>
public class CommunicationSettingsModel
{
    /// <summary>
    /// Gets or sets the ID of the settings.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the settings.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the data selector that returns the receivers.
    /// If the receivers are coming from a different source, this value can be 0.
    /// </summary>
    public int ReceiversDataSelectorId { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the query (from the table wiser_query) that returns the receivers.
    /// If the receivers are coming from a different source, this value can be 0.
    /// </summary>
    public int ReceiversQueryId { get; set; }
    
    /// <summary>
    /// Gets or sets the list of static receivers.
    /// If the receivers come from a different source, this can be empty.
    /// </summary>
    public List<string> ReceiversList { get; set; }
    
    /// <summary>
    /// Gets or sets the type of communication (ie E-mail, SMS etc).
    /// </summary>
    public CommunicationTypes Type { get; set; }
    
    /// <summary>
    /// Gets or sets the trigger type for when to send the communication.
    /// </summary>
    public SendTriggerTypes SendTriggerType { get; set; }
    
    /// <summary>
    /// Gets or sets the start date of periodic communications.
    /// </summary>
    public DateTime TriggerStart { get; set; }
    
    /// <summary>
    /// Gets or sets the end date of periodic communications.
    /// </summary>
    public DateTime TriggerEnd { get; set; }
    
    /// <summary>
    /// Gets or sets the time of day that the communication should be sent.
    /// </summary>
    public DateTime TriggerTime { get; set; }
    
    /// <summary>
    /// Gets or sets the period value of the trigger. If the trigger is set to weekly, then this is after how many weeks the communication should be sent.
    /// </summary>
    public int TriggerPeriodValue { get; set; }
    
    /// <summary>
    /// Gets or sets the type of periodic trigger (daily, weekly etc).
    /// </summary>
    public TriggerPeriodTypes TriggerPeriodType { get; set; }
    
    /// <summary>
    /// Gets or sets the week day(s) that this trigger should be activated, when <see cref="TriggerPeriodType"/> is set to <see cref="TriggerPeriodTypes.Week"/> or <see cref="TriggerPeriodTypes.Month"/>.
    /// </summary>
    public TriggerWeekDays TriggerWeekDays { get; set; }
    
    /// <summary>
    /// Gets or sets the e-mail specific settings.
    /// Only applicable if <see cref="Type"/> is set to <see cref="CommunicationTypes.Email"/>.
    /// </summary>
    public EmailCommunicationSettingsModel EmailSettings { get; set; }
    
    /// <summary>
    /// Gets or sets the SMS specific settings.
    /// Only applicable if <see cref="Type"/> is set to <see cref="CommunicationTypes.Sms"/>.
    /// </summary>
    public SmsCommunicationSettingsModel SmsSettings { get; set; }
    
    /// <summary>
    /// Gets or sets when this communication has been processed last.
    /// </summary>
    public DateTime LastProcessed { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the user that added this communication.
    /// </summary>
    public string AddedBy { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time of when this communication was created.
    /// </summary>
    public DateTime AddedOn { get; set; }
}