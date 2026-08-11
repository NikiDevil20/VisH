using VisH.Model.Enums;

namespace VisH.Model.Calculation.CalculationProperties;

public class GaussianParameters
{
    public Functionals Functional { get; set; }
    public BasisSets BasisSet { get; set; }
    public int Memory { get; set; }
    public int NProc { get; set; }
    public string OptionalKeywords { get; set; }
    public JobTypes Jobtype { get; set; }
    public Solvents Solvent { get; set; }
    public string? Keywords { get; set; }

    public GaussianParameters(
        Functionals functional,
        BasisSets basisSet,
        int memory,
        int nProc,
        string optionalKeywords,
        JobTypes jobtype,
        Solvents solvent)
    {
        Functional = functional;
        BasisSet = basisSet;
        Memory = memory;
        NProc = nProc;
        OptionalKeywords = optionalKeywords;
        Jobtype = jobtype;
        Solvent = solvent;
    }
}