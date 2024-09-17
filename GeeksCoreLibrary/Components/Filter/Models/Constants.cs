namespace GeeksCoreLibrary.Components.Filter.Models;

 public static class Constants
{
    internal const string ValueSplitter = ",";

    internal const string TemplateFull = @"<section>
	    <div class=""container"">
            <div class=""row"">
                <div class=""col col--1"">
                    <input type=""checkbox"" id=""filter1"" class=""hidden"">
                    <div class=""filters"" data-direction=""horizontal"">
                        <div class=""filter-title"">
                            <label for=""filter1"" class=""filter-label""><strong>Filters</strong></label>
                            <a href=""{url}"" class=""filter-reset"">Reset all filters</a>
                        </div>
                        <div class=""filter-panel"">
                            {summary}
                            <div class=""filter-container"">
                                {filters}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </section>";

    internal const string TemplateFilterGroup = @"<div class=""filter-widget multi-filter"">
        <input type=""checkbox"" id=""filter-{name}"" class=""hidden"">
        <label for=""filter-{name}"">
            <strong>{name}</strong>
            <svg class=""icon"">
                <use xlink:href=""/icons/compiled/compiled-icons.svg#icon-arrow-right"">
                </use>
            </svg>
        </label>

        <select class=""filter-select"" multiple=""multiple"" name=""{name}"" data-item=""{name}"">
            <option value="">Maak een keuze</option>
            {items:Raw}
        </select>
    </div>";

    internal const string TemplateSingleSelectItem = @"<option value=""1"" data-initially-set=""false"" data-filterurl="""" data-imageurl="""" data-hexcolor="""" data-colorimageurl="""" href=""{url}"">{filtername} ({count})</option>";

    internal const string TemplateSingleSelectItemSelected = @"<option value=""1"" data-initially-set=""false"" data-filterurl="""" data-imageurl="""" data-hexcolor="""" data-colorimageurl="""" href=""{url}"" selected>{filtername} ({count})</option>";

    internal const string TemplateMultiSelectItem = @"<option value=""1"" data-initially-set=""false"" data-filterurl="""" data-imageurl="""" data-hexcolor="""" data-colorimageurl="""" href=""{url}"">{filtername} ({count})</option>";

    internal const string TemplateMultiSelectItemSelected = @"<option value=""1"" data-initially-set=""false"" data-filterurl="""" data-imageurl="""" data-hexcolor="""" data-colorimageurl="""" href=""{url}"" selected>{filtername} ({count})</option>";

    internal const string TemplateSlider = @"<div class=""filter-widget price-filter"" data-name=""{filtergroup}"" data-min=""{minValue}"" data-max=""{maxValue}"" data-currentmin=""{selectedMin}"" data-currentmax=""{selectedMax}"">
        <input type=""checkbox"" id=""filter-{filtergroup}"" class=""hidden"">
        <label for=""filter-{filtergroup}"">
            <strong>{filtergroup}</strong>
            <svg class=""icon"">
                <use xlink:href=""/icons/compiled/compiled-icons.svg#icon-arrow-right"">
                </use>
            </svg>
        </label>

        <div class=""filter-block"">
            <div class=""price-slider"">
                <div class""multi-range"">
                    <div class=""range-background""></div>
                    <div class=""range-indicator""></div>
                    <input class=""min"" type=""range"" value=""{minValue}"" />
                    <input class=""max"" type=""range"" value=""{maxValue}"" />
                </div>
                <div class=""range-numbers"">
                    <input class=""from"" id=""minvalue"" value=""{selectedMin}"" type=""number"" />
                    <input class=""to"" id=""maxvalue"" value=""{selectedMax}"" type=""number"" />
                </div>
            </div>
        </div>
    </div>";

    internal const string TemplateSummary = @"<div class=""filter-results"">
        <!-- <a href=""{url}"">Wis alle</a> -->
        {items:Raw}
    </div>";

    internal const string TemplateSummaryFilterGroup = @"<div class=""filter-results"">
        <!-- <a href=""{url}"">Wis alle</a> -->
        {selectedvalues:Raw}
    </div>";

    internal const string TemplateSummaryFilterGroupItem = @"<a href=""{url}"" class=""filter-result"">
        <span>{name}</span>
        <svg class=""icon"">
            <use xlink:href=""/icons/compiled/compiled-icons.svg#close""></use>
        </svg>
    </a>";
}