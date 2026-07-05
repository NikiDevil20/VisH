using System.Text;

namespace GaussianTool.Objects;


public class Molecule(string name, List<Atom> atoms, int charge, int multiplicity)
{
    private readonly string _name = name;
    private readonly IReadOnlyList<Atom> _atoms = atoms.ToList();
    private readonly int _charge = charge;
    private readonly int _multiplicity = multiplicity;

    public override string ToString()
    {
        return $"{_name}\n\n{_charge} {_multiplicity}\n{CoordsToString()}";
    }

    private string CoordsToString()
    {
        var sb = new StringBuilder();

        foreach (var atom in _atoms)
            sb.AppendLine(atom.ToString());

        return sb.ToString();
    }
}