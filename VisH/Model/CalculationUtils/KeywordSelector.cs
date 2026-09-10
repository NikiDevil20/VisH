using VisH.Model.Enums;

namespace VisH.Model.CalculationUtils;

public class KeywordSelector
{
    private Solvents? _solvent; 
    private JobTypes? _jobType;
    private State? _state;

    public Dictionary<string, string> GetKeywords(
        BundledConstructionParameters bundledParameters)
    {
        

        _solvent = bundledParameters.Solvent;
        _jobType = bundledParameters.JobType;
        _state = bundledParameters.State;
        
        List<string> keywords = new List<string>();
        List<string> linkKeywords = new List<string>();
        string? scanContext = null;
        
        if (!string.IsNullOrWhiteSpace(bundledParameters.OptionalKeywords))
        {
            keywords.Add(bundledParameters.OptionalKeywords.Trim());
        }

        switch (_jobType)
        { 
            case JobTypes.GeometryOptimization:
                keywords = GeometryOptimizationKeywords(keywords);
                linkKeywords.AddRange(["freq", "geom=AllCheck", "Guess=TCheck"]);
                if (_solvent != Solvents.None)
                {
                    linkKeywords.Add("SCRF=Check");
                }
                linkKeywords.AddRange(["GenChk", "Test"]);
                break;
            case JobTypes.TimeDependant:
                 keywords = TimeDependantKeywords(keywords);
                break;
            case JobTypes.NaturalTransitionOrbitals:
                keywords = NaturalTransitionOrbitalsKeywords(keywords);
                break;
            case JobTypes.PotentialScan:
                keywords = PotentialScanKeywords(keywords);
                scanContext = bundledParameters.ScanContext ?? scanContext;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_jobType), _jobType, "Invalid calculation type");
        }

        var keywordArgument = string.Join(" ", keywords.Where(k => !string.IsNullOrWhiteSpace(k)));
        
        var keywordGroups = new Dictionary<string, string>
        {
            { "keywords", keywordArgument },
            { "linkKeywords", string.Join(" ", linkKeywords) },
            { "scanContext", scanContext ?? "" }
        };
        
        return keywordGroups;
    }
    
    private List<string> GeometryOptimizationKeywords(List<string> keywords)
    {
        keywords.Add("opt");
        
        switch (_state)
        {
            case State.S0:
                keywords.Add("pop=full");
                keywords.Add("GFInput");
                break;
            
            case State.S1 or State.S2 or State.S3:
                break;
            
            case State.T1 or State.T2 or State.T3:
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), _state, "Invalid state");
            
        }
        return keywords;
    }
    
    private List<string> TimeDependantKeywords(List<string> keywords)
    {
        switch (_state)
        {
            case State.S0:
                break;
            
            case State.S1 or State.S2 or State.S3:
                break;
            
            case State.T1 or State.T2 or State.T3:
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), _state, "Invalid state");
            
        }
        return keywords;
    }
    
    private List<string> NaturalTransitionOrbitalsKeywords(List<string> keywords)
    {
        switch (_state)
        {
            case State.S0:
                break;
            
            case State.S1 or State.S2 or State.S3:
                break;
            
            case State.T1 or State.T2 or State.T3:
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), _state, "Invalid state");
            
        }
        return keywords;
    }
    
    private List<string> PotentialScanKeywords(List<string> keywords)
    {
        switch (_state)
        {
            case State.S0:
                break;
            
            case State.S1 or State.S2 or State.S3:
                break;
            
            case State.T1 or State.T2 or State.T3:
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), _state, "Invalid state");
            
        }
        return keywords;
    }
    
    
    
    

}