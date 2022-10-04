using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.GclReplacements.Models
{
    public class StringReplacementVariable
    {
        /// <summary>
        /// The full match string, including the variable prefix and suffix.
        /// </summary>
        public string MatchString { get; set; }

        /// <summary>
        /// The variable name as it was found in the template string. The formatters have been removed from this string.
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Gets or sets the original variable name. This is the same as <see cref="VariableName"/>, but with the formatters part intact.
        /// </summary>
        public string OriginalVariableName { get; set; }

        /// <summary>
        /// A list of formatters that were found on the variable.
        /// </summary>
        public List<string> Formatters { get; } = new();
        
        /// <summary>
        /// The default value of the variable when given.
        /// </summary>
        public string DefaultValue { get; set; }
    }
}
