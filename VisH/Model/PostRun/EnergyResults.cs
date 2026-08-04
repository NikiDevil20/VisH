namespace VisH.Model.PostRun;

public class EnergyResults : CalcResults
{
    public double Scf { get; }

    public EnergyResults(double[] scfEnergies)
    {
        Scf = scfEnergies[^1]; // Assuming the last value in the array is the SCF energy
    }

    public new Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>()
        {
            { "Total Energy / eV", Scf.ToString("F2") },
            { "Total Energy / Hartree", ElectronVoltToHartree(Scf).ToString("F2") }
        };
    }


}
