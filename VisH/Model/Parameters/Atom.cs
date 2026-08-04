using System.Globalization;

namespace VisH.Model.Parameters;

public class Atom
{
    public string Element { get; set; }
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; }

    public override string ToString()
    {
        string _x = x.ToString(CultureInfo.InvariantCulture);
        string _y = y.ToString(CultureInfo.InvariantCulture);
        string _z = z.ToString(CultureInfo.InvariantCulture);
        
        if (_z == "0")
        {
            _z = "0.0";
        }

        return $"" +
               $"{Element} " +
               $"{_x} " +
               $"{_y} " +
               $"{_z}" +
               $"";

    }
}