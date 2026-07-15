using System.Globalization;
using System.Text.Json;

namespace GaussianTool.Model;

public class Atom
{
    public string Element { get; set; }
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; }

    public override string ToString()
    {
        return $"" +
               $"{Element} " +
               $"{x.ToString(CultureInfo.InvariantCulture)} " +
               $"{y.ToString(CultureInfo.InvariantCulture)} " +
               $"{z.ToString(CultureInfo.InvariantCulture)}" +
               $"";

    }
}