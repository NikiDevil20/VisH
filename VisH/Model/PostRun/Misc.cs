namespace VisH.Model.PostRun;

public class Misc
{
    public double Dipole { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Misc(double dipole, double x, double y, double z)
    {
        Dipole = dipole;
        X = x;
        Y = y;
        Z = z;
    }
}
