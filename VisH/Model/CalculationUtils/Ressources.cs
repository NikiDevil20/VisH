using System.Text.Json.Serialization;
using VisH.Model.GeneralUtils;

namespace VisH.Model.CalculationUtils;

public class Ressources
{
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public TimeSpan Walltime { get; set; }
    public float UsedCpu { get; set; }
    public float UsedMemory { get; set; }
}