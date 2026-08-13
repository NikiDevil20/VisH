using System.Text;
using System.Text.Json;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils;
using VisH.Model.Parameters;

namespace VisH.Model.CalculationProperties;


public class Molecule
{
    public string MoleculeName { get; set; }
    private Atom[] Atoms { get; set; }
    public State State { get; set; }
    public int Charge { get; set; }
    public string SmilesString { get; set; }

    public Molecule(
        BundledConstructionParameters parameters)
    {
        Atoms = GetAtoms(parameters.SmilesString).ToArray();
        MoleculeName = parameters.MoleculeName;
        Charge = int.Parse(parameters.Charge);
        State = parameters.State;
        SmilesString = parameters.SmilesString;
    }
    
    public override string ToString()
    {
        var multiplicity = State switch
        {
            State.S0 or State.S1 or State.S2 or State.S3 => "1",
            State.T1 or State.T2 or State.T3 => "3",
            _ => throw new ArgumentOutOfRangeException(nameof(State), State.ToString(), null)
        };
        
        return $"{MoleculeName}\r\n\n{Charge} {multiplicity}\r\n{CoordsToString()}";
    }

    private string CoordsToString()
    {
        var sb = new StringBuilder();

        foreach (var atom in Atoms)
            sb.AppendLine(atom.ToString());

        return sb.ToString();
    }

    private List<Atom> GetAtoms(string smilesString)
    {
        var jsonString = SmilesToJson(smilesString);
        
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
