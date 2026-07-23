namespace GaussianTool.Model.PostRun;

public class Orbitals
{
    public double HOMO { get; set; }
    public double LUMO { get; set; }
    public double Gap { get; set; }
    
    public Orbitals(double[] energies, int nHomo)
    {
        HOMO = energies[nHomo];
        LUMO = energies[nHomo+1];
        Gap = LUMO - HOMO;
    }
}