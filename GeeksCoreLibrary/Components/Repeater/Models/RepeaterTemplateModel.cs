namespace GeeksCoreLibrary.Components.Repeater.Models
{
    public class RepeaterTemplateModel
    {
        /// <summary>
        /// This template will be rendered if the data source contains no data for this layer.
        /// </summary>
        public string NoDataTemplate { get; set; } = "";

        /// <summary>
        /// This template will be rendered once every time at the start of rendering this layer.
        /// </summary>
        public string HeaderTemplate { get; set; } = "";

        /// <summary>
        /// This template will be rendered once every time at the end of rendering this layer.
        /// </summary>
        public string FooterTemplate { get; set; } = "";

        /// <summary>
        /// This template will be rendered for every item/row of this layer.
        /// </summary>
        public string ItemTemplate { get; set; } = "";
        
        public string SelectedItemTemplate { get; set; } = "";

        /// <summary>
        /// This template will be rendered in between every 2 items/rows of this layer.
        /// </summary>
        public string BetweenItemsTemplate { get; set; } = "";
    }
}
