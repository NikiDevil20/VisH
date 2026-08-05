using VisH.Model.Enums;

namespace VisH.Model.Setup;

public class KeywordSelector
{
    private Solvents _solvent; 
    private CalculationType _calculationType;
    private State _state;

    public string GetKeywords(
        Solvents solvent,
        CalculationType calculationType,
        State state,
        string[] optionalKeywords)
    {
        _solvent = solvent;
        _calculationType = calculationType;
        _state = state;

        string[] keywords;

        switch (_calculationType)
        { 
            case CalculationType.GeometryOptimization:
                keywords = GeometryOptimizationKeywords();
                break;
            case CalculationType.TimeDependant:
                 keywords = TimeDependantKeywords();
                break;
            case CalculationType.NaturalTransitionOrbitals:
                keywords = NaturalTransitionOrbitalsKeywords();
                break;
            case CalculationType.PotentialScan:
                keywords = PotentialScanKeywords();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_calculationType), _calculationType, "Invalid calculation type");
        }

        var keywordArgument = string.Join(" ", keywords);
        
        return keywordArgument;
    }
    
    private string[] GeometryOptimizationKeywords()
    {
        string[] keywords;
        switch (_state)
        {
            case State.S0:
                keywords = [];
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
    
    private string[] TimeDependantKeywords()
    {
        string[] keywords = Array.Empty<string>();
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
    private string[] NaturalTransitionOrbitalsKeywords()
    {
        string[] keywords = Array.Empty<string>();
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
    
    private string[] PotentialScanKeywords()
    {
        string[] keywords = Array.Empty<string>();
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