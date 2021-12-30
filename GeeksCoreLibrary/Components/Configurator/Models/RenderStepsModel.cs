using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class RenderStepsModel
    {
        public int MainStep { get; set; }
        public int Step { get; set; }
        public int SubStep { get; set; }
        public string DependentValue { get; set; }
    }
}
