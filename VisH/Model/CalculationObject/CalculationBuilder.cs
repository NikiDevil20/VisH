using VisH.Model.CalculationProperties;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.PostRun;

namespace VisH.Model.CalculationObject;

public static class CalculationBuilder
{
    
    public static Calculation GeometryOptimization(
        BundledConstructionParameters bundledParameters,
        SshService sshService)
    {
        var metaData = new MetaData();
        var gaussianParameters = new GaussianParameters(bundledParameters);
        var molecule = new Molecule(bundledParameters, bundledParameters.GeometryAtoms);
        
        var calculation = new Calculation(sshService);
        
        calculation.AddMetaData(metaData);
        calculation.AddGaussianParameters(gaussianParameters);
        calculation.AddMolecule(molecule);
        
        calculation.AddPaths();
        calculation.GaussianParameters.AddKeywords(bundledParameters);

        return calculation;
    }

    public static Calculation TimeDependant(
        BundledConstructionParameters bundledConstructionParameters,
        string geometryOptimizationJobId,
        string geometryOptimizationChkPath,
        SshService sshService,
        bool useExistingCheckpoint = false)
    {
        if (string.IsNullOrWhiteSpace(geometryOptimizationJobId))
        {
            throw new ArgumentException("Geometry optimization job id is required for a time-dependent calculation.", nameof(geometryOptimizationJobId));
        }

        if (string.IsNullOrWhiteSpace(geometryOptimizationChkPath))
        {
            throw new ArgumentException("The geometry optimization checkpoint path is required for a time-dependent calculation.", nameof(geometryOptimizationChkPath));
        }

        var molecule = new Molecule(
            bundledConstructionParameters,
            bundledConstructionParameters.GeometryAtoms);
        var metaData = new MetaData();
        var gaussianParameters = new GaussianParameters(bundledConstructionParameters)
        {
            OldChkPath = useExistingCheckpoint ? null : geometryOptimizationChkPath,
            DependencyJobId = useExistingCheckpoint ? null : geometryOptimizationJobId
        };

        var calculation = new Calculation(sshService);
        calculation.AddGaussianParameters(gaussianParameters);
        calculation.AddMolecule(molecule);
        calculation.AddMetaData(metaData);
        calculation.AddPaths();
        
        calculation.GaussianParameters.AddKeywords(bundledConstructionParameters);

        return calculation;
    }
}
