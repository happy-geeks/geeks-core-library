using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class CustomParameters
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of the parameter.
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// Dependencies
        /// </summary>
        public List<string> Dependencies { get; set; }

        /// <summary>
        /// The query used to calculate the value.
        /// </summary>
        public string Query { get; set; }
    }
}
