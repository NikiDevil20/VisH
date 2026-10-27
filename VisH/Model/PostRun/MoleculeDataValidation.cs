using System.IO;
using System.Globalization;

namespace VisH.Model.PostRun;

public sealed record GeometryAtom(string Element, double X, double Y, double Z);

public sealed record GeometryValidationResult(
    IReadOnlyList<GeometryAtom> Atoms,
    bool IsValid,
    string Message)
{
    public static GeometryValidationResult Invalid(string message) =>
        new([], false, message);
}

public sealed record CubeValidationResult(
    bool IsValid,
    string Message);

public static class MoleculeDataValidation
{
    private const double BohrToAngstrom = 0.529177210903;
    private const double CoordinateTolerance = 0.001;

    public static GeometryValidationResult ParseGeometry(string coordinates)
    {
        var atoms = new List<GeometryAtom>();
        var lines = coordinates.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 ||
                !IsElement(parts[0]) ||
                !TryParseFinite(parts[1], out var x) ||
                !TryParseFinite(parts[2], out var y) ||
                !TryParseFinite(parts[3], out var z))
            {
                return GeometryValidationResult.Invalid(
                    "Die finale Gaussian-Geometrie enthält eine ungültige Atomzeile.");
            }

            atoms.Add(new GeometryAtom(parts[0], x, y, z));
        }

        return atoms.Count == 0
            ? GeometryValidationResult.Invalid("Keine gültige finale Gaussian-Geometrie verfügbar.")
            : new GeometryValidationResult(atoms, true, string.Empty);
    }

    public static bool HasSameAtomOrder(
        IReadOnlyList<GeometryAtom> geometry,
        IReadOnlyList<string>? moleculeElements)
    {
        if (moleculeElements is null || moleculeElements.Count != geometry.Count)
        {
            return false;
        }

        return geometry
            .Select(atom => atom.Element)
            .SequenceEqual(moleculeElements, StringComparer.OrdinalIgnoreCase);
    }

    public static CubeValidationResult ValidateCube(
        string cubePath,
        IReadOnlyList<GeometryAtom> geometry)
    {
        try
        {
            using var reader = new StreamReader(cubePath);
            if (reader.ReadLine() is null || reader.ReadLine() is null)
            {
                return new(false, "Cube-Datei enthält keine vollständigen Kommentarzeilen.");
            }

            var atomLine = ReadValues(reader);
            if (atomLine is null || atomLine.Length < 4 ||
                !TryParseInt(atomLine[0], out var signedAtomCount) ||
                signedAtomCount == 0)
            {
                return new(false, "Cube-Header enthält keine gültige Atomanzahl.");
            }

            var atomCount = Math.Abs(signedAtomCount);
            var gridLines = new string[3][];
            for (var i = 0; i < 3; i++)
            {
                var values = ReadValues(reader);
                if (values is null || values.Length < 4 ||
                    !TryParseInt(values[0], out var gridCount) ||
                    gridCount == 0 ||
                    !TryParseFinite(values[1], out _) ||
                    !TryParseFinite(values[2], out _) ||
                    !TryParseFinite(values[3], out _))
                {
                    return new(false, "Cube-Header enthält ungültige Gitterdaten.");
                }

                gridLines[i] = values;
            }

            if (atomCount != geometry.Count)
            {
                return new(false, "Die Atomanzahl der Cube-Datei stimmt nicht mit der Geometrie überein.");
            }

            var gridUnitSigns = gridLines
                .Select(values => int.Parse(values[0], CultureInfo.InvariantCulture) < 0)
                .Distinct()
                .ToArray();
            if (gridUnitSigns.Length != 1)
            {
                return new(false, "Cube-Datei verwendet uneinheitliche Einheiten im Gitterheader.");
            }

            var usesAngstrom = gridUnitSigns[0];

            for (var i = 0; i < atomCount; i++)
            {
                var values = ReadValues(reader);
                if (values is null || values.Length < 5 ||
                    !TryParseInt(values[0], out var atomicNumber) ||
                    atomicNumber <= 0 ||
                    !TryParseFinite(values[1], out _) ||
                    !TryParseFinite(values[2], out var x) ||
                    !TryParseFinite(values[3], out var y) ||
                    !TryParseFinite(values[4], out var z))
                {
                    return new(false, "Cube-Datei enthält ungültige Atomdaten.");
                }

                var element = ElementFromAtomicNumber(atomicNumber);
                if (element is null ||
                    !string.Equals(element, geometry[i].Element, StringComparison.OrdinalIgnoreCase))
                {
                    return new(false, "Elementsymbole der Cube-Datei stimmen nicht mit der Geometrie überein.");
                }

                if (!usesAngstrom)
                {
                    x *= BohrToAngstrom;
                    y *= BohrToAngstrom;
                    z *= BohrToAngstrom;
                }

                if (Math.Abs(x - geometry[i].X) > CoordinateTolerance ||
                    Math.Abs(y - geometry[i].Y) > CoordinateTolerance ||
                    Math.Abs(z - geometry[i].Z) > CoordinateTolerance)
                {
                    return new(false, "Atomkoordinaten der Cube-Datei stimmen nicht mit der Geometrie überein.");
                }
            }

            return new(true, string.Empty);
        }
        catch (IOException exception)
        {
            return new(false, $"Cube-Datei konnte nicht gelesen werden: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return new(false, $"Cube-Datei konnte nicht gelesen werden: {exception.Message}");
        }
    }

    private static string[]? ReadValues(StreamReader reader) =>
        reader.ReadLine()?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static bool TryParseFinite(string value, out double result) =>
        double.TryParse(value.Replace('D', 'E').Replace('d', 'e'),
            NumberStyles.Float, CultureInfo.InvariantCulture, out result) &&
        double.IsFinite(result);

    private static bool TryParseInt(string value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool IsElement(string value)
    {
        var index = Array.FindIndex(ElementSymbols, symbol =>
            string.Equals(symbol, value, StringComparison.OrdinalIgnoreCase));
        return index >= 0;
    }

    private static string? ElementFromAtomicNumber(int atomicNumber) =>
        atomicNumber is > 0 and <= 118 ? ElementSymbols[atomicNumber - 1] : null;

    private static readonly string[] ElementSymbols =
    [
        "H", "He", "Li", "Be", "B", "C", "N", "O", "F", "Ne", "Na", "Mg", "Al", "Si",
        "P", "S", "Cl", "Ar", "K", "Ca", "Sc", "Ti", "V", "Cr", "Mn", "Fe", "Co", "Ni",
        "Cu", "Zn", "Ga", "Ge", "As", "Se", "Br", "Kr", "Rb", "Sr", "Y", "Zr", "Nb", "Mo",
        "Tc", "Ru", "Rh", "Pd", "Ag", "Cd", "In", "Sn", "Sb", "Te", "I", "Xe", "Cs", "Ba",
        "La", "Ce", "Pr", "Nd", "Pm", "Sm", "Eu", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb",
        "Lu", "Hf", "Ta", "W", "Re", "Os", "Ir", "Pt", "Au", "Hg", "Tl", "Pb", "Bi", "Po",
        "At", "Rn", "Fr", "Ra", "Ac", "Th", "Pa", "U", "Np", "Pu", "Am", "Cm", "Bk", "Cf",
        "Es", "Fm", "Md", "No", "Lr", "Rf", "Db", "Sg", "Bh", "Hs", "Mt", "Ds", "Rg", "Cn",
        "Nh", "Fl", "Mc", "Lv", "Ts", "Og"
    ];
}
