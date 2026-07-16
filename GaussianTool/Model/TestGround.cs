namespace GaussianTool.Model;

public class TestGround
{
    public TestGround()
    {
        Molecule mol = new Molecule("c1ccccc1", "benzene", "0", "1");
        
        Console.WriteLine(mol);
    }
}