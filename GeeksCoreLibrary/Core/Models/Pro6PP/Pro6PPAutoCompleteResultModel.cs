using System.Collections.Generic;

namespace GeeksCoreLibrary.Core.Models.Pro6PP
{
    public class Pro6PPAutoCompleteResultModel
    {
        public IEnumerable<Pro6PPAddressModel> Results { get; set; }

        public string Status { get; set; }

        public Pro6PPErrorModel Error { get; set; }
    }
}
