using System.Text.Json;

namespace GaussianTool.Model.PostRun;

public class CalcResults
{
    public string? NHomo { get; set; }
    public string? SCFEnergy { get; set; }
    public string? HomoEnergy { get; set; }
    public string? LumoEnergy { get; set; }
    public string? Dipole { get; set; }
    
    public CalcResults Load(string jobId)
    {
        // get json from python cclib.
        string jsonContent = "Test";
        
        CalcResults? calcResults = JsonSerializer.Deserialize<CalcResults>(jsonContent);
        
        if (calcResults == null)
        {
            throw new Exception("Failed to deserialize CalcResults");
        }

        return calcResults;
    }
}