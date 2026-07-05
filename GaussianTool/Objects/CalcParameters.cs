namespace GaussianTool.Objects;

public class CalcParameters(int proc, int ram, string functional, string basisSet, string state, List<string> keywords)
{
    private int Proc { get; } = proc;
    private int Ram { get; } = ram;
    public string Functional { get; } = functional;
    public string BasisSet { get; } = basisSet;
    public string State { get; } = state;
    private List<string> Keywords { get; } = keywords;

    public override string ToString()
    {
        string returnString = $"%NProcShared={Proc}\n";
        returnString += "%Chk=gauss.chk\n";
        returnString += $"mem={Ram}GB\n";
        returnString += $"#p {Functional} {BasisSet}\n";
        returnString += string.Join(" ", Keywords) + "\n \n";
        
        return returnString;
    }
}