namespace GeeksCoreLibrary.Modules.DataSelector.Models;

public class HavingRow
{
    public Field Key { get; set; }

    public string Operator { get; set; }

    public object Value { get; set; }
}