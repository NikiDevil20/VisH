namespace GaussianTool.Model.PostRun;

public class Frequency
{
    public int NImag { get; }
    public double Lowest { get; }
    public double Highest { get; }

    public Frequency(double[] frequencies)
    {
        if (frequencies.Length == 0)
        {
            NImag = 0;
            Lowest = 0;
            Highest = 0;
            return;
        }

        NImag = frequencies.Count(f => f < 0);
        Lowest = frequencies.Min();
        Highest = frequencies.Max();
    }
    
    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>()
        {
            { "NImag", NImag.ToString() },
            { "Lowest", Lowest.ToString() },
            { "Highest", Highest.ToString() }
        };
    }
}