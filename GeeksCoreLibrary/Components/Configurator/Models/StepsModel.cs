using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class StepsModel
    {
        /// <summary>
        /// Gets or sets the ID of the step.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the step that is visible to the user.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the selected value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the name of the selected value that it is visible to the user.
        /// </summary>
        public string ValueName { get; set; }

        /// <summary>
        /// Gets or sets the type (step or substep).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or the number of the main step, where this step is a part of.
        /// </summary>
        public int MainStep { get; set; }

        /// <summary>
        /// Gets or sets the current step number.
        /// </summary>
        public int Step { get; set; }

        /// <summary>
        /// Gets or sets the sub step number, if this step is a substep.
        /// </summary>
        public int SubStep { get; set; }

        /// <summary>
        /// Gets or sets any extra data.
        /// </summary>
        public List<Dictionary<string, object>> ExtraData { get; set; }
    }
}
