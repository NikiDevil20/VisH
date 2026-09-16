using System.Runtime.InteropServices.JavaScript;

namespace VisH.Model.CalculationUtils;

public class Dates
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime SubmittedDate { get; set; }

    public Dates()
    {
        var time = DateTime.Now;
        SubmittedDate = time;
    }
    
    // Needs more complex logic to fetch start and endtimes from qstat.
    public void SetStartTime()
    {
        var time = DateTime.Now;
        StartDate = time;
    }

    public void SetEndTime()
    {
        var time = DateTime.Now;
        EndDate = time;
    }
    
}