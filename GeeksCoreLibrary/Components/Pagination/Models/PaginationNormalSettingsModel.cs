using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Pagination.Models
{
    internal class PaginationNormalSettingsModel
    {
        [DefaultValue("pagenr")]
        internal string PageNumberVariableName { get; set; }

        [DefaultValue(10U)]
        internal uint ItemsPerPage { get; set; }

        [DefaultValue("...")]
        internal string DotsTemplate { get; set; }

        [DefaultValue(1U)]
        internal uint MinPagesAtStart { get; set; }

        [DefaultValue(1U)]
        internal uint MinPagesAtEnd { get; set; }

        [DefaultValue(4)]
        internal int DotsOffset { get; set; }

        [DefaultValue("{summary} {pagination}")]
        internal string FullTemplate { get; set; }
    }
}
