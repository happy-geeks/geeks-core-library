using System;
using System.Diagnostics.CodeAnalysis;
using GeeksCoreLibrary.Core.Cms;

namespace GeeksCoreLibrary.Components.Pagination.Models;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PaginationLegacySettingsModel : CmsSettingsLegacy
{
    public string PageVariableName { get; set; }

    public int AantalItemsPerPagina { get; set; }

    public int MaxAantal { get; set; }

    public int MaxAantalVoorHuidige { get; set; }

    public int MaxAantalNaHuidige { get; set; }

    public bool CombineMaxBeforeAndAfter { get; set; }

    public bool AddPageQueryStringToLinkFormat { get; set; }

    public bool RemoveFirstPageFromURL { get; set; }

    public bool AddDotsToFirstAndLast { get; set; }

    public bool AddFilterSelectionQueryPart { get; set; }

    public string FullTemplate { get; set; }

    public string Summarytemplate { get; set; }

    public string EerstePagina { get; set; }

    public string VorigePagina { get; set; }

    public string PaginaTemplate { get; set; }

    public string SelectedPaginaTemplate { get; set; }

    public string VolgendePagina { get; set; }

    public string LaatstePagina { get; set; }

    public string TussenPaginaTemplate { get; set; }

    public string LinkFormat { get; set; }

    public string DotsTemplate { get; set; }

    public string DotsOffset { get; set; }

    public string Header { get; set; }

    public string Footer { get; set; }

    public bool showPagingOnOnePage { get; set; }

    public bool AddRelPrevNextLinkTags { get; set; }

    public PaginationCmsSettingsModel ToSettingsModel()
    {
        var paginationCmsSettingsModel = new PaginationCmsSettingsModel
        {
            PageNumberVariableName = PageVariableName,
            ItemsPerPage = Convert.ToUInt32(AantalItemsPerPagina),
            MaxPages = Convert.ToUInt32(MaxAantal),
            MaxPagesBeforeCurrent = Convert.ToUInt32(MaxAantalVoorHuidige),
            MaxPagesAfterCurrent = Convert.ToUInt32(MaxAantalNaHuidige),
            CombineMaxBeforeAndAfter = CombineMaxBeforeAndAfter,
            AddPageQueryStringToLinkFormat = AddPageQueryStringToLinkFormat,
            RemoveFirstPageFromUrl = RemoveFirstPageFromURL,
            AddDotsToFirstAndLast = AddDotsToFirstAndLast,
            FullTemplate = (String.IsNullOrWhiteSpace(FullTemplate) ? "{summary} {pagination}" : FullTemplate).Replace("{pagination}", $"{Header ?? ""}{{pagination}}{Footer ?? ""}", StringComparison.OrdinalIgnoreCase),
            SummaryTemplate = Summarytemplate,
            FirstPageTemplate = EerstePagina,
            PreviousPageTemplate = VorigePagina,
            PageTemplate = PaginaTemplate,
            CurrentPageTemplate = SelectedPaginaTemplate,
            NextPageTemplate = VolgendePagina,
            LastPageTemplate = LaatstePagina,
            InBetweenTemplate = TussenPaginaTemplate,
            LinkFormat = LinkFormat,
            DotsTemplate = DotsTemplate,
            DotsOffset = Convert.ToInt32(DotsOffset),
            RenderForSinglePage = showPagingOnOnePage,
            AddPreviousAndNextLinkRelationTags = AddRelPrevNextLinkTags,

            // Inherited items from abstract parent
            HandleRequest = HandleRequest,
            DataQuery = SQLQuery
        };

        return paginationCmsSettingsModel;
    }

    public static PaginationLegacySettingsModel FromSettingsModel(PaginationCmsSettingsModel settings)
    {
        return new()
        {
            PageVariableName = settings.PageNumberVariableName,
            AantalItemsPerPagina = Convert.ToInt32(settings.ItemsPerPage),
            MaxAantal = Convert.ToInt32(settings.MaxPages),
            MaxAantalVoorHuidige = Convert.ToInt32(settings.MaxPagesBeforeCurrent),
            MaxAantalNaHuidige = Convert.ToInt32(settings.MaxPagesAfterCurrent),
            CombineMaxBeforeAndAfter = settings.CombineMaxBeforeAndAfter,
            AddPageQueryStringToLinkFormat = settings.AddPageQueryStringToLinkFormat,
            RemoveFirstPageFromURL = settings.RemoveFirstPageFromUrl,
            AddDotsToFirstAndLast = settings.AddDotsToFirstAndLast,
            FullTemplate = settings.FullTemplate,
            Summarytemplate = settings.SummaryTemplate,
            EerstePagina = settings.FirstPageTemplate,
            VorigePagina = settings.PreviousPageTemplate,
            PaginaTemplate = settings.PageTemplate,
            SelectedPaginaTemplate = settings.CurrentPageTemplate,
            VolgendePagina = settings.NextPageTemplate,
            LaatstePagina = settings.LastPageTemplate,
            TussenPaginaTemplate = settings.InBetweenTemplate,
            LinkFormat = settings.LinkFormat,
            DotsTemplate = settings.DotsTemplate,
            DotsOffset = settings.DotsOffset.ToString(),
            AddRelPrevNextLinkTags = settings.AddPreviousAndNextLinkRelationTags,

            showPagingOnOnePage = settings.RenderForSinglePage,
            HandleRequest = settings.HandleRequest,
            SQLQuery = settings.DataQuery
        };
    }
}