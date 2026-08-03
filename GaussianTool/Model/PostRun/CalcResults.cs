using System.IO;
using System.IO.Enumeration;
using System.Text.Json;
using System.Text.Json.Serialization;
using GaussianTool.Model.FileHandling;

namespace GaussianTool.Model.PostRun;



public class CalcResults
{
    public double[] AllFreqs { get; init; } = [];
    public double[] MoEnergies { get; init; } = [];
    public double[] ScfEnergies { get; init; } = [];
    public string CoordResults { get; init; } = string.Empty;
    public int NHomo { get; init; } = 0;


    private PathObject? _calculationDirectory;

    [JsonIgnore]
    public PathObject CalculationDirectory =>
        _calculationDirectory ?? throw new InvalidOperationException(
            "CalculationDirectory not set.");
    
    [JsonIgnore] public MetaData MetaData => new(CalculationDirectory);

    [JsonIgnore] public EnergyResults Energy => new(ScfEnergies);

    [JsonIgnore] public Frequency Frequency => new(AllFreqs);

    [JsonIgnore] public Orbitals Orbitals => new(MoEnergies, NHomo);
    

    public static CalcResults LoadFromJson(PathObject calculationDirectory)
    {
        CalcResults? results;
        try
        {
            var json = File.ReadAllText(calculationDirectory.GetFileWithEnding(".json")?.WindowsPath ?? string.Empty);
            results = JsonSerializer.Deserialize<CalcResults>(json);
        }
        catch (FileNotFoundException e)
        {
            throw new FileNotFoundException("JSON file not found.", e);
        }
        catch (InvalidOperationException e)
        {
            throw new InvalidOperationException("Failed to load calculation results.", e);
        }
        
        if (results == null)
        {
            throw new InvalidOperationException("Failed to load calculation results.");
        }

        results._calculationDirectory = calculationDirectory;
        
        return results;
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