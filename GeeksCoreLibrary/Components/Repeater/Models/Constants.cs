namespace GeeksCoreLibrary.Components.Repeater.Models;

public class Constants
{
    // Legacy placeholders for Repeater templates.
    public const string LegacyRowNumberVariableName = "volgnr";
    public const string LegacyResultCountVariableName = "ResultCount";
    public const string LegacyUniqueResultCountVariableName = "UniqueResultCount";

    // Placeholders used in Repeater templates.
    public const string RowNumberVariableName = "RowNumber";
    public const string RowIndexVariableName = "RowIndex";
    public const string RowCountVariableName = "RowCount";
    public const string DistinctRowCountVariableName = "DistinctRowCount";

    public const string SubLayerPlaceholder = "{SubLayer}";
    public const string FiltersPlaceholder = "{Filters}";
    public const string PageLimitPlaceholder = "{page_limit}";
}