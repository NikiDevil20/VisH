namespace VisH.Model;

public class DataGridItem
{
    public string Property { get; set; }
    public string Value { get; set; }
    
    public DataGridItem(string property, string value)
    {
        Property = property;
        Value = value;
    }
}
