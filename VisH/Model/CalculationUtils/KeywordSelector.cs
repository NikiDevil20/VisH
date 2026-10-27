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
        switch (_state)
        {
            case State.S0:
                AddSolventKeyword(keywords);
                keywords.Add("opt scf=xqc pop=full GFInput");
                break;
            
            case State.S1 or State.S2 or State.S3:
                AddSolventKeyword(keywords);
                keywords.Add("opt scf=xqc TD=(singlet,NStates=6,root=1)");
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
                AddSolventKeyword(keywords, "correctedLR");
                keywords.Add("TD=(singlet,NStates=25,root=1) geom=check guess=read");
                break;
            case State.S1 or State.S2 or State.S3:
                AddSolventKeyword(keywords, "ExternalIteration,correctedLR");
                keywords.Add("TD=(singlet,NStates=6,root=1) geom=check guess=read");
                break;
            
            case State.T1 or State.T2 or State.T3:
                keywords.Add("TD=(NStates=10,Triplets)");
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(_state), _state, "Invalid state");
            
        }
        return keywords;
    }

    private void AddSolventKeyword(List<string> keywords, string? options = null)
    {
        if (_solvent is null or Solvents.None)
        {
            return;
        }

        var suffix = string.IsNullOrWhiteSpace(options) ? string.Empty : $",{options}";
        keywords.Add($"scrf=(smd,solvent={_solvent}{suffix})");
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