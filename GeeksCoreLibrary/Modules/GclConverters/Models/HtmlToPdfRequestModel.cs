using EvoPdf;

namespace GeeksCoreLibrary.Modules.GclConverters.Models
{
    /// <summary>
    /// A model for a request to convert HTML to a PDF.
    /// </summary>
    public class HtmlToPdfRequestModel
    {
        /// <summary>
        /// Gets or sets the HTML that should be converted to a PDF.
        /// </summary>
        public string Html { get; set; }

        /// <summary>
        /// Gets or sets the Wiser 2 item ID that contains the background image for the PDF.
        /// </summary>
        public ulong ItemId { get; set; }

        /// <summary>
        /// Gets or sets the Wiser 2 property name that contains the background image for the PDF.
        /// </summary>
        public string BackgroundPropertyName { get; set; }

        /// <summary>
        /// Gets or sets any extra PDF document options. This can be any property known by the used HTML to PDF converter. See their documentation for all the options.
        /// This should be in the following format: "OptionA:ValueA;OptionB:ValueB"
        /// </summary>
        public string DocumentOptions { get; set; }

        /// <summary>
        /// Gets or sets the HTML that should be used as the header of every page in the PDF.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the HTML that should be used as the footer of every page in the PDF.
        /// </summary>
        public string Footer { get; set; }

        /// <summary>
        /// Gets or sets the file name for the generated PDF.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets whether to save the file in database (wiser_itemfile) instead of on disk.
        /// </summary>
        public bool SaveInDatabase { get; set; }

        /// <summary>
        /// Gets or sets the orientation for the PDF. Default is Portrait.
        /// </summary>
        public PdfPageOrientation? Orientation { get; set; } = PdfPageOrientation.Portrait;
    }
}
