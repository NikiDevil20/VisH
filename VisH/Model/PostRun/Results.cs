namespace VisH.Model.PostRun;

public class Results
{
    public int? NHomo { get; set; }
    public int? NLumo { get; set; }
    public int? NImag { get; set; }
    public int? NImagFromFrequencies { get; set; }
    public bool? OptimizationConverged { get; set; }
    public bool? NormalTermination { get; set; }
    public bool OrbitalDataAvailable { get; set; }
    public bool Unrestricted { get; set; }
    public double[] AllFreqs { get; set; } = [];
    public double[] MoEnergies { get; set; } = [];
    public double[] ScfEnergies { get; set; } = [];
    public string CoordResults { get; set; } = string.Empty;
    public bool GeometryValid { get; set; }
    public string GeometryMessage { get; set; } = string.Empty;
    public List<Peak> Peaks { get; set; } = [];
    public int Version { get; set; } = 1;
}