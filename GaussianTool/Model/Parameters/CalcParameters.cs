using System.Text;

namespace GaussianTool.Model;

public class CalcParameters
{
    public string CalcType { get; init; }
    public string Proc { get; init; }
    public string Ram { get; init; }
    public string Functional { get; init; }
    public string BasisSet { get; init; }
    public string State { get; init; }
    public string Time { get; init; }
    public string? Solvent { get; init; }
    private string[] Keywords { get; set; }
    public CalcParameters? Link { get; set; }

    private bool _isLink;

    public CalcParameters(string calcType, string proc, string ram, string functional, string basisSet, string state,
        string? solvent, string time, string[] keywords, bool isLink = false)
    {
        CalcType = calcType;
        Proc = proc;
        Ram = ram;
        Functional = functional;
        BasisSet = basisSet;
        State = state;
        Time = time;
        Keywords = keywords;
        Solvent = solvent;
        _isLink = isLink;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"%NProcShared={Proc}");
        sb.AppendLine("%Chk=gauss.chk");
        sb.AppendLine($"%mem={Ram}GB");
        sb.AppendLine($"#p {Functional} {BasisSet}");
        if (Solvent != null && !_isLink) sb.Append($"scrf=(smd,solvent={Solvent}) ");
        sb.AppendLine(string.Join(" ", Keywords));

        return sb.ToString();
    }
}