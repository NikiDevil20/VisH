namespace GaussianTool.Model;

public class TestGround
{
    public TestGround()
    {
        List<Atom> atoms = new List<Atom>();
        
        PythonBridge bridge = new PythonBridge();

        string jsonString = bridge.ExecuteScript("CoordBuilder.py", ["c1ccccc1"]);
        
        Console.WriteLine(jsonString);

        Molecule mol = new Molecule(jsonString, "benzene", "0", "1");
        
        Console.WriteLine(mol);
    }
}