namespace GeeksCoreLibrary.Modules.Branches.Enumerations
{
    /// <summary>
    /// The possible modes for copying items of the given entity type to a new branch.
    /// </summary>
    public enum CreateBranchEntityModes
    {
        /// <summary>
        /// Copy all items of the given entity type.
        /// </summary>
        Everything,
        /// <summary>
        /// Don't copy any items of the given entity type.
        /// </summary>
        Nothing,
        /// <summary>
        /// Copy a certain amount of random items of the given entity type.
        /// </summary>
        Random,
        /// <summary>
        /// Copy a certain amount of recent items of the given entity type.
        /// </summary>
        Recent,
        /// <summary>
        /// Copy all items of the given entity type, that were created before a certain date.
        /// </summary>
        CreatedBefore,
        /// <summary>
        /// Copy all items of the given entity type, that were created after a certain date. 
        /// </summary>
        CreatedAfter,
        /// <summary>
        /// Copy all items of the given entity type, that were created between two dates.
        /// </summary>
        CreatedBetween
    }
}