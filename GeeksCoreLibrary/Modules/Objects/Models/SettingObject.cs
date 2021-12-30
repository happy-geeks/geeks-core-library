namespace GeeksCoreLibrary.Modules.Objects.Models
{
    public class SettingObject
    {
        /// <summary>
        /// Gets or sets the type number. Objects are divided by types, this is the ID of that type (from the table "objecttypes").
        /// This can be "-1" for generic objects.
        /// </summary>
        public int TypeNumber { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }
    }
}
