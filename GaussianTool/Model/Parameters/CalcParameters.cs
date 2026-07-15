using System.Text;

namespace GaussianTool.Model;

public class CalcParameters(string calcType, int proc, int ram, string functional, string basisSet, string state, string? solvent, 
    string time, List<string> keywords)
{
    public string CalcType { get; } = calcType;
    public int Proc { get; } = proc;
    public int Ram { get; } = ram;
    public string Functional { get; } = functional;
    public string BasisSet { get; } = basisSet;
    public string State { get; } = state;
    public string Time { get; } = time;
    private List<string> Keywords { get; } = keywords;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"%NProcShared={Proc}");
        sb.AppendLine("%Chk=gauss.chk");
        sb.AppendLine($"mem={Ram}GB");
        sb.AppendLine($"#p {Functional} {BasisSet}");
        if (solvent != null) sb.Append($"scrf=(smd,solvent={solvent}) ");
        sb.AppendLine(string.Join(" ", Keywords));
        sb.AppendLine(" ");

        return sb.ToString();
    }
}