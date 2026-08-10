using VisH.Model.Enums;

namespace VisH.Model.Setup;

public class GaussianParameters
{
    public State State { get; set; }
    public CalculationType CalculationType { get; set; }
    public int Memory { get; set; }
    public int NProcs { get; set; }
    public BasisSets BasisSet { get; set; }
    public Functionals Functional { get; set; }
    
}