using System.Text;

namespace GaussianTool.Objects;


public class Molecule(string name, List<Atom> atoms, int charge, int multiplicity)
{
    public readonly string Name = name;
    private readonly IReadOnlyList<Atom> _atoms = atoms.ToList();
    public readonly int Charge = charge;
    public readonly int Multiplicity = multiplicity;

    public override string ToString()
    {
        return $"{Name}\n\n{Charge} {Multiplicity}\n{CoordsToString()}";
    }

    private string CoordsToString()
    {
        var sb = new StringBuilder();

        foreach (var atom in _atoms)
            sb.AppendLine(atom.ToString());

        return sb.ToString();
    }
}