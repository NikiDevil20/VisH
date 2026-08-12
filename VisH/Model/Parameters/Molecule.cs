using System.Text;
using System.Text.Json;
using VisH.Model.FileHandling;
using VisH.Model.Parameters;

namespace VisH.Model;


public class Molecule
{
    public string Name;
    private Atom[] _atoms;
    public string Charge;
    public string Multiplicity;
    public string SmilesString;

    public Molecule(string smilesString, string name, string charge, string multiplicity)
    {
        _atoms = GetAtoms(smilesString).ToArray();
        Name = name;
        Charge = charge;
        Multiplicity = multiplicity;
        SmilesString = smilesString;
    }
    
    public override string ToString()
    {
        return $"{Name}\r\n\n{Charge} {Multiplicity}\r\n{CoordsToString()}";
    }

    private string CoordsToString()
    {
        var sb = new StringBuilder();

        foreach (var atom in _atoms)
            sb.AppendLine(atom.ToString());

        return sb.ToString();
    }

    private List<Atom> GetAtoms(string smilesString)
    {
        string jsonString = SmilesToJson(smilesString);
        
        List<Atom>? atoms = JsonSerializer.Deserialize<List<Atom>>(jsonString);
        if (atoms == null)
            throw new ArgumentException("Invalid JSON list");

        
        return atoms;
        
    }

    private string SmilesToJson(string smilesString)
    {
        var bridge = new PythonBridge();
        
        if (!IsValidSmiles(smilesString))
            throw new ArgumentException("Invalid SMILES string");

        string jsonString = bridge.ExecuteScript("CoordBuilder.py", [smilesString]);
        
        return jsonString;
    }

    public void DrawSvg(string directory, PythonBridge pythonBridge)
    {
        const string scriptName = "DrawPngFromSmiles.py";
        var direcotryPath = directory;
        
        string[] pythonArguments = [SmilesString, direcotryPath];
        
        string output = pythonBridge.ExecuteScript(scriptName, pythonArguments);
        Console.WriteLine(output);

    }

    public static bool IsValidSmiles(string smilesString)
    {
        var bridge = new PythonBridge();
        
        string errorMessage = bridge.ExecuteScript("SmilesValidation.py", [smilesString]);
        return (errorMessage.Contains("valid"));
    }
}
