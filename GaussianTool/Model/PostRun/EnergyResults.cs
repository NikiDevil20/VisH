namespace GaussianTool.Model.PostRun;

public class EnergyResults
{
    public double Scf { get; }

    public EnergyResults(double[] scfEnergies)
    {
        Scf = scfEnergies[^1]; // Assuming the last value in the array is the SCF energy
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>()
        {
            { "SCF", Scf.ToString() }
        };
    }


}