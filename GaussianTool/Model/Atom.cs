namespace GaussianTool.Model;

public class Atom(string element, double x, double y, double z, bool isFixed = false)
{
    private string Element { get; } =  element;
    private double X { get; } = x;
    private double Y { get; } = y;
    private double Z { get; } = z;
    private bool IsFixed { get; } = isFixed;

    public override string ToString()
    {
        return $"{Element} {X} {Y} {Z}";
    }
}