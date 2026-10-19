namespace VisH.Model.PostRun;

public class Results
{
    public int NHomo { get; set; }
    public double[] AllFreqs { get; set; } = [];
    public double[] MoEnergies { get; set; } = [];
    public double[] ScfEnergies { get; set; } = [];
    public string CoordResults { get; set; } = string.Empty;
    public List<Peak> Peaks { get; set; } = [];
    public int Version { get; set; } = 1;
}