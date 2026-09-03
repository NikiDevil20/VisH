using System.Text;
using System.Text.Json.Serialization;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils;

namespace VisH.Model.CalculationProperties;

public class GaussianParameters
{
    public Functionals Functional { get; set; }
    public BasisSets BasisSet { get; set; }
    public int Memory { get; set; }
    public int NProc { get; set; }
    public JobTypes Jobtype { get; set; }
    public Solvents Solvent { get; set; }
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public TimeSpan MaxWalltime { get; set; }
    public string? Keywords { get; set; }
    public string? LinkKeywords { get; set; }
    public string? ScanContext { get; set; }
    
    [JsonConstructor]
    public GaussianParameters()
    {
    }

    public GaussianParameters(
        BundledConstructionParameters bundledConstructionParameters)
    {
        Functional = bundledConstructionParameters.Functional;
        BasisSet = bundledConstructionParameters.BasisSet;
        Memory = bundledConstructionParameters.Memory;
        NProc = bundledConstructionParameters.NProcs;
        Jobtype = bundledConstructionParameters.JobType;
        Solvent = bundledConstructionParameters.Solvent;
        MaxWalltime = bundledConstructionParameters.MaxWalltime;
        Keywords = null;
        LinkKeywords = null;
        ScanContext = null;
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"%NProcShared={NProc}");
        sb.AppendLine("%Chk=gauss.chk");
        sb.AppendLine($"%mem={Memory}GB");
        sb.AppendLine($"#p {Functional} {BasisSet}");
        if (Solvent != Solvents.None) sb.Append($" scrf=(smd,solvent={Solvent}) ");
        sb.AppendLine(Keywords);

        return sb.ToString();
    }

    public string ToLink()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"%NProcShared={NProc}");
        sb.AppendLine("%Chk=gauss.chk");
        sb.AppendLine($"%mem={Memory}GB");
        sb.AppendLine($"#p {Functional} {BasisSet} ");
        sb.Append(LinkKeywords);

        return sb.ToString();
    }

    public string GetScanContext()
    {
        return ScanContext ?? "";
    }

    public void AddKeywords(BundledConstructionParameters bundledParameters)
    {
        var keywordSelector = new KeywordSelector();
        var keywordsDict = keywordSelector.GetKeywords(bundledParameters);
        
        Keywords = keywordsDict["keywords"];
        LinkKeywords = keywordsDict["linkKeywords"];
        ScanContext = keywordsDict["scanContext"];
    }
}