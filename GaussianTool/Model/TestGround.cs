namespace GaussianTool.Model;

public class TestGround
{
    public TestGround()
    {
        PythonBridge bridge = new PythonBridge();

        string output = bridge.ExecuteScript("CoordBuilder.py", ["Hello World!"]);
        
        Console.WriteLine(output);

    }
}