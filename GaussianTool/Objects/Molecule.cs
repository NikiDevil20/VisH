using System.Text;

namespace GaussianTool.Objects;


public class Molecule(string name, Atom[] atoms, int charge, int multiplicity)
{
    public readonly string Name = name;
    private readonly Atom[] _atoms = atoms;
    public readonly int Charge = charge;
    public readonly int Multiplicity = multiplicity;

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
}