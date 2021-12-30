namespace GeeksCoreLibrary.Components.Repeater.Models
{
    public class RepeaterTemplateModel
    {
        public string NoDataTemplate { get; set; } = "";
        public string HeaderTemplate { get; set; } = "";
        public string FooterTemplate { get; set; } = "";
        public string ItemTemplate { get; set; } = "";
        public string SelectedItemTemplate { get; set; } = "";
        public string BetweenItemsTemplate { get; set; } = "";
    }
}
