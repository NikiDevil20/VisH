namespace GaussianTool.Model;

public class MoleculeProperties
{
    public string Energy { get; set; }
    public string HomoEnergy { get; set; }
    public string LumoEnergy { get; set; }
    public string DipoleMoment { get; set; }
    
    public MoleculeProperties(string energy, string homoEnergy, string lumoEnergy, string dipoleMoment)
    {
        Energy = energy;
        HomoEnergy = homoEnergy;
        LumoEnergy = lumoEnergy;
        DipoleMoment = dipoleMoment;
    }
    
}