using System.Windows.Media;
using VisH.Model.CalculationObject;
using VisH.Model.Enums;

namespace VisH.Model;

public class CalcStatus
{
    public JobState JobState { get; set; }
    public string JobName { get; set; }
    public string JobId { get; set; }
    public Brush LampColor { get; set; }
    
    public static CalcStatus Create(Calculation calculation)
    {
        var calcStatus = new CalcStatus
        {
            JobState = calculation.MetaData?.JobState ?? JobState.Unknown,
            JobName = calculation.MetaData?.JobName ?? string.Empty,
            JobId = calculation.MetaData?.JobId ?? string.Empty,
            LampColor = calculation.MetaData?.JobState switch
            {
                JobState.Successful => new SolidColorBrush(Colors.Green),
                JobState.Running => new SolidColorBrush(Colors.Yellow),
                JobState.Queue => new SolidColorBrush(Colors.Orange),
                JobState.Failed => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Gray)
            }
        };

        return calcStatus;
    }
}