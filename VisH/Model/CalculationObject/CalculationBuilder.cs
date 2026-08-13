using VisH.Model.CalculationProperties;
using VisH.Model.CalculationUtils;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.PostRun;

namespace VisH.Model.CalculationObject;

public static class CalculationBuilder
{
    
    public static Calculation Build(BundledConstructionParameters bundledParameters)
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
}