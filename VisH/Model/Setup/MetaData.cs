using System.IO;
using VisH.Model.Enums;
using VisH.Model.FileHandling;

namespace VisH.Model.PostRun;

public class MetaData
{
    public string JobName { get; set; }
    public string JobId { get; set; }
    public JobState JobState { get; set; }

    public MetaData(PathObject calculationDirectory)
    {
        JobState = JobState.Queue;
    }

}
