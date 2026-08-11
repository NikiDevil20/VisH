using VisH.Model.Enums;

namespace VisH.Model.Calculation.CalculationUtils;

public static class NameGenerator
{
    public static string GetCalculationName(
        string baseName,
        JobTypes jobType,
        State state,
        string customName="")
    {
        string fullName;
        string suffix = "";

        switch (jobType)
        {
            case JobTypes.GeometryOptimization:
                suffix = "opt";
                break;
            case JobTypes.TimeDependant:
                switch (state)
                {
                    case State.S0:
                        suffix = "abs";
                        break;
                    case State.S1 or State.S2 or State.S3:
                        suffix = "flu";
                        break;
                    case State.T1 or State.T2 or State.T3:
                        suffix = "pho";
                        break;
                }
                break;
            case JobTypes.PotentialScan:
                suffix = "scan";
                break;
        }
        
        if (customName != "")
        {
            fullName = string.Join("_", [customName, baseName, suffix]);
        }
        else
        {
            fullName = string.Join("_", [baseName, suffix]);
        }

        return fullName;
    }
    
    public static string GetUniqueCalculationName(string nonUniqueName)
    {
        if (nonUniqueName.EndsWith("abs") || nonUniqueName.EndsWith("flu") ||
            nonUniqueName.EndsWith("pho") || nonUniqueName.EndsWith("opt") || 
            nonUniqueName.EndsWith("scan"))
        {
            // hat noch kein inkrement
            return nonUniqueName + "_1";
        }

        int index = nonUniqueName.LastIndexOf("_");
        string baseName = nonUniqueName.Substring(0, index);
        string incrementString = nonUniqueName.Substring(index + 1);
        int currentIncrement = int.Parse(incrementString);

        return baseName + $"_{currentIncrement + 1}";
    }
    
    
    
}