namespace VisH.Model.PostRun;

public class Frequency
{
    public string NImag { get; }
    public string Lowest { get; }
    public string Highest { get; }

    public Frequency(double[] frequencies)
    {
        if (frequencies.Length == 0)
        {
            NImag = "not detected";
            Lowest = "not detected";
            Highest = "not detected";
            return;
        }

        NImag = frequencies.Count(f => f < 0).ToString();
        Lowest = frequencies.Min().ToString("F2");
        Highest = frequencies.Max().ToString("F2");
    }
    
    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>()
        {
            { "NImag", NImag },
            { "Smallest / cm?¹", Lowest },
            { "Largest / cm?¹", Highest }
        };
    }
}
