using VisH.Model.FileHandling;
using VisH.Model.PostRun;

namespace VisH.Model.Calculation.CalculationObject;

public static class CalculationBuilder
{
    
    public static Calculation GeometryOptimization(Dictionary<string, string> parameters)
    {
        var calculationDirectory = parameters["calculationDirectory"];
        var metaData = new MetaData(new PathObject(calculationDirectory));
        
        
        
        
        var calculation = new Calculation();
        calculation.AddMetaData(metaData);
        
    }
}