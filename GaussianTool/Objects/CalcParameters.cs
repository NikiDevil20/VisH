namespace GaussianTool.Objects;

public class CalcParameters(string functional, string basisSet)
{
    public string Functional { get; set; } = functional;
    public string BasisSet { get; set; } = basisSet;
    
    public override string ToString()
    {
        return $"{Functional} / {BasisSet}";
    }
}