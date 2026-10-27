using VisH.Model.Enums;
using VisH.Model.GeneralUtils;

namespace VisH.Model.CalculationUtils;

public record BundledConstructionParameters(
    string MoleculeName,
    string? SmilesString,
    string Charge,
    State State,
    int Memory,
    int NProcs,
    Functionals Functional,
    BasisSets BasisSet,
    JobTypes JobType,
    Solvents Solvent,
    TimeSpan MaxWalltime,
    string? GeometryOptimizationJobId,
    Atom[]? GeometryAtoms,
    string? OptionalKeywords,
    bool? DispersionCorrection,
    string? ScanContext
    );