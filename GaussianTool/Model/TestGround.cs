using GaussianTool.Model.Hilbert;
using GaussianTool.Model.PostRun;

namespace GaussianTool.Model;

public class TestGround
{
    public TestGround()
    {
        string dirPath = "C:\\Users\\nikla\\OneDrive - Heinrich-Heine-Universitat Dusseldorf\\Dokumente\\R" +
                         "echnungen\\AntiAnti_PT_fusBT\\S0\\AntiAnti_PT_fusBT_Abs";
        MetaData md = new MetaData(dirPath);
        Console.WriteLine(md.JobId);
    }
}