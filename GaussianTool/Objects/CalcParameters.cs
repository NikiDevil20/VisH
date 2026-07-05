namespace GaussianTool.Objects;

public class CalcParameters(int proc, int ram, string functional, string basisSet, string state, string? solvent, 
    string time, List<string> keywords)
{
    public int Proc { get; } = proc;
    public int Ram { get; } = ram;
    public string Functional { get; } = functional;
    public string BasisSet { get; } = basisSet;
    public string State { get; } = state;
    public string Time { get; } = time;
    private List<string> Keywords { get; } = keywords;

    public override string ToString()
    {
        string returnString = $"%NProcShared={Proc}\n";
        returnString += "%Chk=gauss.chk\n";
        returnString += $"mem={Ram}GB\n";
        returnString += $"#p {Functional} {BasisSet}\n";
        if (solvent != null) returnString += $"scrf=(smd,solvent={solvent}) ";
        returnString += string.Join(" ", Keywords) + "\n \n";
        
        return returnString;
    }
}