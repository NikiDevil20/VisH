using VisH.Model.CalculationUtils;
using VisH.Model.Enums;

namespace VisH.Model.CalculationProperties;

public class MetaData
{
    public Dates Dates { get; set; }
    public Ressources Ressources { get; set; }
    public string? JobName { get; set; }
    public JobState JobState { get; set; }
    public string? JobId { get; set; }

    public MetaData()
    {
        Dates = new Dates();
        Ressources = new Ressources();
        JobState = JobState.InPreparation;
    }
    
}
