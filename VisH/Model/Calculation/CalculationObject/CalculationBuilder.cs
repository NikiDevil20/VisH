using VisH.Model.Calculation.CalculationProperties;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.PostRun;

namespace VisH.Model.Calculation.CalculationObject;

public static class CalculationBuilder
{
    
    public static Calculation GeometryOptimization(Dictionary<string, string> parameters)
    {
        var calculationDirectory = parameters["calculationDirectory"];
        var metaData = new MetaData();

        var sshService = new SshService();
        
        
        var calculation = new Calculation(sshService);
        calculation.AddMetaData(metaData);


        return calculation;
    }
}