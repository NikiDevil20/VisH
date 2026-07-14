namespace GaussianTool.Model.Parameters;

public class States
{
    public string Name { get; init; }
    public string ExcitedN { get; init; }
    public string Multiplicity { get; init; }

    public States(string excitedN, string multiplicityChar)
    {
        Name = multiplicityChar + excitedN;
        ExcitedN = excitedN;
        if (multiplicityChar == "S")
            Multiplicity = "1";
        else
            Multiplicity = "3";
    }
    
    public override string ToString()
    {
        return Name;
    }
}