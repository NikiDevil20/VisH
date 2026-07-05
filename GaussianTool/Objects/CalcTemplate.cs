namespace GaussianTool.Objects;

public class CalcTemplate(string title, CalcParameters parameters)
{
    public string Title { get; } =  title;
    public CalcParameters Parameters { get; } = parameters;
    public string Description => $"{parameters.State}, {parameters.Functional}, {parameters.BasisSet}";

    public override string ToString()
    {
        return $"'{Title}: {Description}'";
    }
}