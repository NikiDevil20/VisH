using VisH.Model.CalculationProperties;
using VisH.Model.CalculationUtils;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.PostRun;

namespace VisH.Model.CalculationObject;

public static class CalculationBuilder
{
    
    public static Calculation GeometryOptimization(BundledConstructionParameters bundledParameters)
    {
        var metaData = new MetaData();
        var sshService = new SshService();
        var gaussianParameters = new GaussianParameters(bundledParameters);
        var molecule = new Molecule(bundledParameters);
        
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
        string geometryOptimizationJobId)
    {
        var jobFinder = new JobFinder();
        var geometryOptimizationJob = jobFinder.GetCalculationByJobId(geometryOptimizationJobId);

        string optimizedGeometry = "geometryOptimizationJob.Results.Coordinates"; // TODO
        var molecule = new Molecule(bundledConstructionParameters, atomCoordinates: optimizedGeometry);
        
        var metaData = new MetaData();
        var sshService = new SshService();
        var gaussianParameters = new GaussianParameters(bundledConstructionParameters);

        var calculation = new Calculation(sshService);
        calculation.AddGaussianParameters(gaussianParameters);
        calculation.AddMolecule(molecule);
        calculation.AddMetaData(metaData);
        calculation.AddPaths();
        
        calculation.GaussianParameters.AddKeywords(bundledConstructionParameters);

        return calculation;
    }
}