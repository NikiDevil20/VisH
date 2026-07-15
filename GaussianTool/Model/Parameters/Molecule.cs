using System.Text;
using System.Text.Json;

namespace GaussianTool.Model;


public class Molecule
{
    public string Name;
    private Atom[] _atoms;
    public int Charge;
    public int Multiplicity;

    public Molecule(string jsonList, string name, string charge, string multiplicity)
    {
        _atoms = GetAtoms(jsonList).ToArray();
        Name = name;
        Charge = int.Parse(charge);
        Multiplicity = int.Parse(multiplicity);
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

    private List<Atom> GetAtoms(string jsonList)
    {
        List<Atom>? atoms = JsonSerializer.Deserialize<List<Atom>>(jsonList);
        if (atoms == null)
            throw new ArgumentException("Invalid JSON list");
        return atoms;
        
    }
}