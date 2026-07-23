using System.Text.Json;
using System.Text.Json.Serialization;

namespace GaussianTool.Model.PostRun;

public class CalcResults
{
    public double[] AllFreqs { get; init; } = [];
    public double[] MoEnergies { get; init; } = [];
    public double[] ScfEnergies { get; init; } = [];
    public string CoordResults { get; init; }
    public int NHomo { get; init; } = 0;
    
    [JsonIgnore]
    public MetaData MetaData { get; init; }
    
    [JsonIgnore]
    public EnergyResults Energy => new(ScfEnergies);
    
    [JsonIgnore]
    public Frequency Frequency => new(AllFreqs);
    
    [JsonIgnore]
    public Orbitals Orbitals => new(MoEnergies, NHomo);
    
    public CalcResults Load(string directoryPath)
    {
        string jsonContent = "Test";
        
        CalcResults? calcResults = JsonSerializer.Deserialize<CalcResults>(jsonContent);
        
        if (calcResults == null)
        {
            throw new Exception("Failed to deserialize CalcResults");
        }

        return calcResults;
    }
    
    public static double HartreeToElectronVolt(double hartree)
    {
        return hartree * 27.2114;
    }
    
    public static double HartreeToNanoMeters(double hartree)
    {
        return 1239.8 / HartreeToElectronVolt(hartree);
    }
}