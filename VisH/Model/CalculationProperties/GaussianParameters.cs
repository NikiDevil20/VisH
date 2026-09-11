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
    public string? OldChkPath { get; set; }
    public string? DependencyJobId { get; set; }
    
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
        OldChkPath = null;
        DependencyJobId = null;
    }
    
    public override string ToString()
    {
        var cfg = Config.Load();
        var sb = new StringBuilder();
        sb.AppendLine($"%NProcShared={NProc}");
        if (!string.IsNullOrWhiteSpace(OldChkPath))
        {
            sb.AppendLine($"%oldchk=/home/{cfg.ClusterUsername}/{OldChkPath}");
        }
        sb.AppendLine("%Chk=gauss.chk");
        sb.AppendLine($"%mem={Memory}GB");
        
        var routeBuilder = new StringBuilder();
        routeBuilder.Append($"#p {Functional} {BasisSet}");
        if (Solvent != Solvents.None)
        {
            routeBuilder.Append($" scrf=(smd,solvent={Solvent})");
        }
        if (!string.IsNullOrWhiteSpace(Keywords))
        {
            routeBuilder.Append($" {Keywords.Trim()}");
        }
        sb.AppendLine(routeBuilder.ToString());

        return sb.ToString();
    }

    public string ToLink()
    {
        var cfg = Config.Load();
        var sb = new StringBuilder();
        sb.AppendLine($"%NProcShared={NProc}");
        if (!string.IsNullOrWhiteSpace(OldChkPath))
        {
            sb.AppendLine($"%oldchk=/home/{cfg.ClusterUsername}/{OldChkPath}");
        }
        sb.AppendLine("%Chk=gauss.chk");
        sb.AppendLine($"%mem={Memory}GB");
        
        var routeBuilder = new StringBuilder();
        routeBuilder.Append($"#p {Functional} {BasisSet}");
        if (!string.IsNullOrWhiteSpace(LinkKeywords))
        {
            routeBuilder.Append($" {LinkKeywords.Trim()}");
        }
        sb.AppendLine(routeBuilder.ToString());

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
        
        Keywords = string.IsNullOrWhiteSpace(keywordsDict["keywords"]) ? null : keywordsDict["keywords"];
        LinkKeywords = string.IsNullOrWhiteSpace(keywordsDict["linkKeywords"]) ? null : keywordsDict["linkKeywords"];
        ScanContext = string.IsNullOrWhiteSpace(keywordsDict["scanContext"]) ? null : keywordsDict["scanContext"];
    }
}