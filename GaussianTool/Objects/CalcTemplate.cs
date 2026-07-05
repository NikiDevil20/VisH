namespace GaussianTool.Objects;

public class CalcTemplate
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public CalcParameters Parameters { get; set; }
    
    public CalcTemplate(string title, CalcParameters parameters)
    {
        Title = title;
        Parameters = parameters;
        Description = parameters.ToString();
    }
}