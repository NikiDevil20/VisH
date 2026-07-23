namespace GaussianTool.Model.PostRun;

public class Frequency
{
    public int NImag { get; }
    public double Lowest { get; }
    public double Highest { get; }

    public Frequency(double[] frequencies)
    {
        NImag = frequencies.Count(f => f < 0);
        Lowest = frequencies.Min();
        Highest = frequencies.Max();
    }
}