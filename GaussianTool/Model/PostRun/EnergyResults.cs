namespace GaussianTool.Model.PostRun;

public class EnergyResults
{
    public double SCF { get; }

    public EnergyResults(double[] scfEnergies)
    {
        SCF = scfEnergies[^1]; // Assuming the last value in the array is the SCF energy
    }

    
}