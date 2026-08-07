using System.IO;
using VisH.Model.FileHandling;

namespace VisH.Model.PostRun;

public class MetaData
{
    public string JobName { get; set; }
    public string JobId { get; set; }

    public MetaData(PathObject calculationDirectory)
    {

    }

}
