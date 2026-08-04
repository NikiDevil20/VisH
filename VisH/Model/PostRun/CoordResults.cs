namespace VisH.Model.PostRun;

public class CoordResults
{
    public string XyzFormat { get; init; }
    
    public CoordResults(string xyzFormat)
    {
        XyzFormat = xyzFormat;
    }
}
